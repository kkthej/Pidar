using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pidar.Areas.Identity.Data;
using Pidar.Data;
using Pidar.Infrastructure;
using Pidar.Models.Submissions;
using Pidar.Services.Assistant;
using Pidar.Services.Submissions;

namespace Pidar.Controllers
{
    /// <summary>
    /// "Make your data FAIR": data providers describe their dataset in a guided form, save it as a draft,
    /// come back later and submit it; Admins and Curators review it and approve (→ a Dataset is created),
    /// ask for changes, or reject.
    ///
    /// Data providers only ever see their own submissions. Nothing here touches the public dataset
    /// tables except Approve.
    /// </summary>
    [Authorize]
    [Route("Submissions")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class SubmissionsController : Controller
    {
        private readonly PidarDbContext _db;
        private readonly SubmissionService _service;
        private readonly SubmissionNotifier _notifier;
        private readonly SubmissionHistory _history;
        private readonly AssistantSettings _assistant;
        private readonly UserManager<PidarUser> _users;
        private readonly ILogger<SubmissionsController> _logger;

        public SubmissionsController(
            PidarDbContext db,
            SubmissionService service,
            SubmissionNotifier notifier,
            SubmissionHistory history,
            AssistantSettings assistant,
            UserManager<PidarUser> users,
            ILogger<SubmissionsController> logger)
        {
            _db = db;
            _service = service;
            _notifier = notifier;
            _history = history;
            _assistant = assistant;
            _users = users;
            _logger = logger;
        }

        private string CurrentUserId => _users.GetUserId(User) ?? "";
        private string CurrentEmail => User.Identity?.Name ?? "";
        private bool IsEditor => AppRoles.IsEditor(User);
        private Actor CurrentActor => new(CurrentUserId, CurrentEmail);

        private string Absolute(string action, object? values = null) =>
            Url.Action(action, "Submissions", values, Request.Scheme) ?? "";

        // ============================================================ data provider

        // GET /Submissions — my contributions
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["ActivePage"] = "Contribute";
            var me = CurrentUserId;
            var mine = await _db.DatasetSubmissions.AsNoTracking()
                .Where(s => s.OwnerUserId == me)
                .OrderByDescending(s => s.UpdatedAt)
                .ToListAsync();
            return View(mine);
        }

        // POST /Submissions/New (optionally copying one of my earlier submissions)
        [HttpPost("New")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> New(int? copyFrom)
        {
            DatasetSubmission? source = null;
            if (copyFrom.HasValue)
            {
                source = await _db.DatasetSubmissions.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == copyFrom.Value && s.OwnerUserId == CurrentUserId);
                if (source == null) return NotFound();
            }

            var draft = await _service.CreateDraftAsync(CurrentUserId, CurrentEmail, source);
            _logger.LogInformation("{Email} started submission {Id}{Copy}", CurrentEmail, draft.Id,
                source != null ? $" (copy of {source.Id})" : "");
            return RedirectToAction(nameof(Edit), new { id = draft.Id });
        }

        // GET /Submissions/5/Edit — the wizard
        [HttpGet("{id:int}/Edit")]
        public async Task<IActionResult> Edit(int id, int? step, bool check = false)
        {
            ViewData["ActivePage"] = "Contribute";
            var s = await FindMineAsync(id);
            if (s == null) return NotFound();
            if (!SubmissionStatus.IsEditableByOwner(s.Status))
                return RedirectToAction(nameof(Details), new { id });

            var answers = SubmissionService.ReadAnswers(s);
            var reviewStep = ContributionForm.Steps.Count;
            return View(new SubmissionEditViewModel
            {
                Submission = s,
                Answers = answers,
                Progress = SubmissionService.Progress(answers),
                Issues = check ? SubmissionService.Validate(answers) : null,
                ActiveStep = check ? reviewStep : Math.Clamp(step ?? s.CurrentStep, 0, reviewStep),
                UnconfirmedSources = await _history.UnconfirmedSourcesAsync(s.Id),
                AssistantMode = _assistant.Mode,
            });
        }

        // POST /Submissions/5/Save — autosave (fetch, returns JSON) or "Save and close" (redirect)
        [HttpPost("{id:int}/Save")]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(2_000_000)]
        public async Task<IActionResult> Save(int id, int? step, bool exit = false)
        {
            var s = await FindMineAsync(id, tracked: true);
            if (s == null) return NotFound();
            if (!SubmissionStatus.IsEditableByOwner(s.Status))
                return WantsJson() ? Conflict(new { ok = false, message = "This submission was already sent and can no longer be changed." })
                                   : RedirectToAction(nameof(Details), new { id });

            await _service.SaveAsync(s, Request.Form, step, CurrentActor);

            if (WantsJson())
            {
                var answers = SubmissionService.ReadAnswers(s);
                return Json(new
                {
                    ok = true,
                    savedAt = s.UpdatedAt.ToString("o"),
                    progress = SubmissionService.Progress(answers).Select(p => new { p.Filled, p.Required }),
                });
            }

            TempData["Message"] = "Your draft was saved. You can continue it at any time from this page.";
            return exit ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Edit), new { id, step });
        }

        // POST /Submissions/5/Submit — save, check, and send for review
        [HttpPost("{id:int}/Submit")]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(2_000_000)]
        public async Task<IActionResult> Submit(int id)
        {
            var s = await FindMineAsync(id, tracked: true);
            if (s == null) return NotFound();
            if (!SubmissionStatus.IsEditableByOwner(s.Status))
                return RedirectToAction(nameof(Details), new { id });

            await _service.SaveAsync(s, Request.Form, ContributionForm.Steps.Count, CurrentActor);

            var issues = SubmissionService.Validate(SubmissionService.ReadAnswers(s));
            if (issues.Count > 0)
            {
                TempData["Error"] = issues.Count == 1
                    ? "One answer still needs your attention before you can submit."
                    : $"{issues.Count} answers still need your attention before you can submit.";
                return RedirectToAction(nameof(Edit), new { id, check = true });
            }

            s.Status = SubmissionStatus.Submitted;
            s.SubmittedAt = DateTime.UtcNow;
            s.UpdatedAt = s.SubmittedAt.Value;
            _history.AddEvent(s.Id, CurrentActor, SubmissionChangeKind.Submitted);
            await _db.SaveChangesAsync();
            _logger.LogInformation("{Email} submitted submission {Id}", CurrentEmail, id);

            await _notifier.SubmittedAsync(s, Absolute(nameof(Index)), Absolute(nameof(Details), new { id }));

            TempData["Message"] = "Thank you! Your dataset description was sent to the PIDAR team. We will email you when it has been reviewed.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Submissions/5/Delete — only drafts that were never submitted
        [HttpPost("{id:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await FindMineAsync(id, tracked: true);
            if (s == null) return NotFound();
            if (s.Status != SubmissionStatus.Draft)
            {
                TempData["Error"] = "Only drafts that were never submitted can be deleted.";
                return RedirectToAction(nameof(Index));
            }

            await _history.ForgetDraftContentAsync(s.Id, CurrentActor);
            _db.DatasetSubmissions.Remove(s);
            await _db.SaveChangesAsync();
            TempData["Message"] = "The draft was deleted.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Submissions/5 — read-only view, for the owner and for reviewers
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            ViewData["ActivePage"] = "Contribute";
            var s = await _db.DatasetSubmissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();

            var isOwner = s.OwnerUserId == CurrentUserId;
            if (!isOwner && !IsEditor) return NotFound();

            // Reviewers never see someone's unsent draft
            if (!isOwner && s.Status == SubmissionStatus.Draft) return NotFound();

            var answers = SubmissionService.ReadAnswers(s);
            return View(new SubmissionDetailsViewModel
            {
                Submission = s,
                Answers = answers,
                Issues = SubmissionService.Validate(answers),
                IsOwner = isOwner,
                CanReview = IsEditor && s.Status == SubmissionStatus.Submitted,
                History = await _history.ListAsync(s.Id),
                UnconfirmedSources = await _history.UnconfirmedSourcesAsync(s.Id),
                AssistantMessageCount = await _db.AssistantMessages.CountAsync(m => m.SubmissionId == s.Id && m.Role == AssistantRole.User),
            });
        }

        // ============================================================ review (Admin / Curator)

        // GET /Submissions/Review?status=Submitted
        [HttpGet("Review")]
        [Authorize(Roles = AppRoles.Editors)]
        public async Task<IActionResult> Review(string? status)
        {
            status = string.IsNullOrWhiteSpace(status) ? SubmissionStatus.Submitted : status;

            // Drafts are private to their owner until submitted
            var visible = _db.DatasetSubmissions.AsNoTracking().Where(s => s.Status != SubmissionStatus.Draft);

            var counts = await visible.GroupBy(s => s.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var list = await visible.Where(s => s.Status == status)
                .OrderByDescending(s => s.SubmittedAt ?? s.UpdatedAt)
                .ToListAsync();

            ViewData["Status"] = status;
            ViewData["Counts"] = counts;
            return View(list);
        }

        // POST /Submissions/5/Approve — create the dataset
        [HttpPost("{id:int}/Approve")]
        [Authorize(Roles = AppRoles.Editors)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var s = await _db.DatasetSubmissions.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();
            if (s.Status != SubmissionStatus.Submitted)
            {
                TempData["Error"] = "Only submissions waiting for review can be approved.";
                return RedirectToAction(nameof(Details), new { id });
            }

            int datasetId;
            try
            {
                datasetId = await _service.ApproveAsync(s, CurrentEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Approving submission {Id} failed", id);
                TempData["Error"] = "The dataset could not be created: " + ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }

            var displayId = await _db.Datasets.Where(d => d.DatasetId == datasetId).Select(d => d.DisplayId).FirstOrDefaultAsync();
            await _history.LogEventAsync(id, CurrentActor, SubmissionChangeKind.Approved, $"published as dataset {displayId}");
            var datasetLink = Url.Action("Details", "Datasets", new { displayId }, Request.Scheme) ?? "";
            await _notifier.ApprovedAsync(s, datasetLink);

            TempData["Message"] = $"Approved: dataset {displayId} was created and is now public.";
            TempData["ApprovedDatasetId"] = datasetId;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST /Submissions/5/RequestChanges — back to the provider with a comment
        [HttpPost("{id:int}/RequestChanges")]
        [Authorize(Roles = AppRoles.Editors)]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> RequestChanges(int id, string? comment) =>
            CloseReviewAsync(id, comment, SubmissionStatus.NeedsRevision);

        // POST /Submissions/5/Reject
        [HttpPost("{id:int}/Reject")]
        [Authorize(Roles = AppRoles.Editors)]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Reject(int id, string? comment) =>
            CloseReviewAsync(id, comment, SubmissionStatus.Rejected);

        private async Task<IActionResult> CloseReviewAsync(int id, string? comment, string newStatus)
        {
            var s = await _db.DatasetSubmissions.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();
            if (s.Status != SubmissionStatus.Submitted)
            {
                TempData["Error"] = "Only submissions waiting for review can be changed.";
                return RedirectToAction(nameof(Details), new { id });
            }
            comment = (comment ?? "").Trim();
            if (comment.Length == 0)
            {
                TempData["Error"] = "Please write a message for the data provider explaining what to change (or why it is not accepted).";
                return RedirectToAction(nameof(Details), new { id });
            }

            s.Status = newStatus;
            s.ReviewerComment = comment.Length > ContributionForm.MaxValueLength ? comment[..ContributionForm.MaxValueLength] : comment;
            s.ReviewedBy = CurrentEmail;
            s.ReviewedAt = DateTime.UtcNow;
            s.UpdatedAt = s.ReviewedAt.Value;
            _history.AddEvent(s.Id, CurrentActor,
                newStatus == SubmissionStatus.NeedsRevision ? SubmissionChangeKind.ChangesRequested : SubmissionChangeKind.Rejected,
                s.ReviewerComment);
            await _db.SaveChangesAsync();
            _logger.LogInformation("{Reviewer} set submission {Id} to {Status}", CurrentEmail, id, newStatus);

            if (newStatus == SubmissionStatus.NeedsRevision)
            {
                await _notifier.ChangesRequestedAsync(s, Absolute(nameof(Edit), new { id }));
                TempData["Message"] = "Changes requested. The data provider was notified by email and can edit the submission again.";
            }
            else
            {
                await _notifier.RejectedAsync(s, Absolute(nameof(Index)));
                TempData["Message"] = "The submission was marked as not accepted and the data provider was notified.";
            }
            return RedirectToAction(nameof(Review));
        }

        // ============================================================ helpers

        private Task<DatasetSubmission?> FindMineAsync(int id, bool tracked = false)
        {
            var q = tracked ? _db.DatasetSubmissions : _db.DatasetSubmissions.AsNoTracking();
            var me = CurrentUserId;
            return q.FirstOrDefaultAsync(s => s.Id == id && s.OwnerUserId == me);
        }

        private bool WantsJson() =>
            Request.Headers.Accept.Any(a => a != null && a.Contains("application/json", StringComparison.OrdinalIgnoreCase));
    }

    public sealed class SubmissionEditViewModel
    {
        public DatasetSubmission Submission { get; init; } = null!;
        public Dictionary<string, string> Answers { get; init; } = new();
        public List<StepProgress> Progress { get; init; } = new();
        /// <summary>Set after a failed Submit: what still needs attention.</summary>
        public List<SubmissionIssue>? Issues { get; init; }
        /// <summary>0..Steps.Count-1 for a form step, Steps.Count for "Review & submit".</summary>
        public int ActiveStep { get; init; }

        /// <summary>Answers filled by the assistant or the Excel import and not changed by hand since: path → source.</summary>
        public Dictionary<string, string> UnconfirmedSources { get; init; } = new();

        /// <summary>Whether (and how) the contribution assistant is offered on this page.</summary>
        public AssistantMode AssistantMode { get; init; } = AssistantMode.Off;

        public string? Value(string path) => Answers.TryGetValue(path, out var v) ? v : null;
    }

    public sealed class SubmissionDetailsViewModel
    {
        public DatasetSubmission Submission { get; init; } = null!;
        public Dictionary<string, string> Answers { get; init; } = new();
        public List<SubmissionIssue> Issues { get; init; } = new();
        public bool IsOwner { get; init; }
        public bool CanReview { get; init; }

        /// <summary>History of the submission, newest first.</summary>
        public List<SubmissionChange> History { get; init; } = new();

        /// <summary>Answers filled by the assistant or the Excel import and not changed by hand since: path → source.</summary>
        public Dictionary<string, string> UnconfirmedSources { get; init; } = new();

        /// <summary>How many questions the provider asked the assistant for this submission.</summary>
        public int AssistantMessageCount { get; init; }

        public string? Value(string path) => Answers.TryGetValue(path, out var v) ? v : null;
    }
}

namespace Pidar.Controllers
{
    /// <summary>One question of the contribution form with its current answer (Views/Submissions/_Field.cshtml).</summary>
    public sealed record SubmissionFieldViewModel(Pidar.Models.Submissions.FormField Field, string? Value);
}
