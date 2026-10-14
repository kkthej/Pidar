using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Pidar.Areas.Identity.Data;
using Pidar.Data;
using Pidar.Models.Submissions;
using Pidar.Services.Assistant;

namespace Pidar.Controllers
{
    /// <summary>
    /// The contribution assistant panel on the "Make your data FAIR" form (Views/Submissions/Edit.cshtml,
    /// wwwroot/js/contribution-assistant.js).
    ///
    /// Only the owner of a submission can talk to the assistant about it, and only while they can edit it
    /// (Draft / NeedsRevision). Limits: <see cref="AssistantOptions.RequestsPerMinute"/> per user (rate limiter
    /// policy) and <see cref="AssistantOptions.MessagesPerDay"/> per user (counted in assistant_message).
    /// </summary>
    [Authorize]
    [Route("Submissions/{id:int}/Assistant")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class AssistantController : Controller
    {
        private readonly PidarDbContext _db;
        private readonly ContributionAssistant _assistant;
        private readonly AssistantSettings _settings;
        private readonly UserManager<PidarUser> _users;
        private readonly ILogger<AssistantController> _logger;

        public AssistantController(
            PidarDbContext db,
            ContributionAssistant assistant,
            AssistantSettings settings,
            UserManager<PidarUser> users,
            ILogger<AssistantController> logger)
        {
            _db = db;
            _assistant = assistant;
            _settings = settings;
            _users = users;
            _logger = logger;
        }

        /// <summary>Message sent when only files are attached, without text.</summary>
        private const string FilesOnlyMessage = "Please read the attached file(s) and suggest answers for the form.";

        // POST /Submissions/5/Assistant — multipart form:
        //   message  text (may be empty when files are attached)
        //   step     section open on screen
        //   answers  JSON { "Section.Property": value } of the answers currently in the form, saved or not
        //   files    0..MaxFilesPerMessage PDFs / pictures
        [HttpPost("")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(AssistantServiceCollectionExtensions.RateLimitPolicy)]
        [RequestSizeLimit(26_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 26_000_000)]
        public async Task<IActionResult> Ask(int id, [FromForm] string? message, [FromForm] int step,
            [FromForm] string? answers, [FromForm] List<IFormFile>? files, CancellationToken ct)
        {
            if (!_settings.IsOn) return NotFound();
            var o = _settings.Options;

            var userId = _users.GetUserId(User) ?? "";
            var s = await _db.DatasetSubmissions.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == userId, ct);
            if (s == null) return NotFound();
            if (!SubmissionStatus.IsEditableByOwner(s.Status))
                return Conflict(new { error = "This submission was already sent and can no longer be changed." });

            files ??= new List<IFormFile>();
            var text = (message ?? "").Trim();
            if (text.Length == 0 && files.Count == 0)
                return BadRequest(new { error = "Please write a message or attach a file." });
            if (text.Length > o.MaxMessageLength)
                return BadRequest(new { error = $"Your message is too long (at most {o.MaxMessageLength:N0} characters). Paste a shorter part, e.g. one section of your methods, or attach the document as a PDF." });
            if (files.Count > o.MaxFilesPerMessage)
                return BadRequest(new { error = $"Please attach at most {o.MaxFilesPerMessage} files to one message." });
            if (files.Sum(f => f.Length) > o.MaxTotalFileBytes)
                return BadRequest(new { error = $"The attached files are too large together (limit {o.MaxTotalFileBytes / 1024 / 1024} MB). Send them in separate messages." });

            if (await _assistant.MessagesTodayAsync(userId, ct) >= o.MessagesPerDay)
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { error = "You have reached today's limit of messages to the assistant. You can keep filling in the form, and the assistant will be available again tomorrow." });

            // Files: real type checked from their content; any problem stops the whole message
            var attachments = new List<AssistantAttachment>();
            foreach (var f in files)
            {
                var (file, error) = await AssistantAttachments.ReadAsync(f, o, ct);
                if (error != null) return BadRequest(new { error });
                attachments.Add(file!);
            }

            // Only the form's own fields are taken from the browser
            var current = new Dictionary<string, string>(StringComparer.Ordinal);
            Dictionary<string, string?>? posted = null;
            try
            {
                posted = string.IsNullOrWhiteSpace(answers) ? null : JsonSerializer.Deserialize<Dictionary<string, string?>>(answers);
            }
            catch (JsonException)
            {
                // answers are context only: without them the assistant still works
            }
            foreach (var (path, value) in posted ?? new Dictionary<string, string?>())
            {
                if (!ContributionForm.FieldsByPath.ContainsKey(path) || string.IsNullOrWhiteSpace(value)) continue;
                var v = value.Trim();
                current[path] = v.Length > ContributionForm.MaxValueLength ? v[..ContributionForm.MaxValueLength] : v;
            }

            try
            {
                var reply = await _assistant.ReplyAsync(s, userId, text.Length > 0 ? text : FilesOnlyMessage, step, current, attachments, ct);
                return Json(new
                {
                    reply = reply.Text,
                    proposals = reply.Proposals,
                    navigateStep = reply.NavigateStep,
                    mode = _settings.Mode.ToString().ToLowerInvariant(),
                });
            }
            catch (AssistantUnavailableException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { error = ex.Message + " Your answers are safe; please try again in a minute, or continue filling in the form yourself." });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return new EmptyResult();   // the browser went away
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Assistant failed on submission {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Something went wrong in the assistant. Your answers are safe; please try again." });
            }
        }

        // GET /Submissions/5/Assistant/History — the conversation so far, to show it again after reopening the page
        [HttpGet("History")]
        public async Task<IActionResult> History(int id, CancellationToken ct)
        {
            if (!_settings.IsOn) return NotFound();

            var userId = _users.GetUserId(User) ?? "";
            var mine = await _db.DatasetSubmissions.AsNoTracking().AnyAsync(x => x.Id == id && x.OwnerUserId == userId, ct);
            if (!mine) return NotFound();

            return Json(new
            {
                messages = await _assistant.TranscriptAsync(id, ct),
                mode = _settings.Mode.ToString().ToLowerInvariant(),
            });
        }
    }
}
