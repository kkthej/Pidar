using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Models.Submissions;

namespace Pidar.Services.Submissions
{
    /// <summary>Who is making a change: the signed-in user (or "system").</summary>
    public sealed record Actor(string UserId, string Email)
    {
        public static readonly Actor System = new("system", "PIDAR");
    }

    /// <summary>
    /// Writes and reads the history of a submission (<see cref="SubmissionChange"/>).
    ///
    /// Field edits are recorded by <see cref="RecordEditsAsync"/>, called from SubmissionService.SaveAsync
    /// with the answers before and after the save. The form autosaves 2 seconds after the last keystroke,
    /// so typing a long answer would leave many intermediate rows; edits of the same field by the same
    /// person from the same source within <see cref="MergeWindow"/> are therefore merged into one row
    /// (first old value → latest new value). A merged edit that ends where it started is removed.
    ///
    /// The rows are added to the DbContext and saved by the caller's SaveChanges, so an answer and its
    /// history row are stored together or not at all.
    /// </summary>
    public sealed class SubmissionHistory
    {
        /// <summary>Edits of one field closer together than this are shown as one change.</summary>
        public static readonly TimeSpan MergeWindow = TimeSpan.FromMinutes(10);

        private readonly PidarDbContext _db;

        public SubmissionHistory(PidarDbContext db) => _db = db;

        // ============================================================ writing

        /// <summary>Adds an event row (Created, Submitted, Approved, …). Saved by the caller's SaveChanges.</summary>
        public void AddEvent(int submissionId, Actor actor, string kind, string? note = null)
        {
            _db.SubmissionChanges.Add(new SubmissionChange
            {
                SubmissionId = submissionId,
                At = DateTime.UtcNow,
                UserId = actor.UserId,
                UserEmail = actor.Email,
                Kind = kind,
                Note = Cut(note),
            });
        }

        /// <summary>Adds an event row and saves it right away.</summary>
        public async Task LogEventAsync(int submissionId, Actor actor, string kind, string? note = null)
        {
            AddEvent(submissionId, actor, kind, note);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Adds one Edited row per answer that differs between <paramref name="before"/> and <paramref name="after"/>
        /// (merging with the person's recent edit of the same field, see class summary).
        /// <paramref name="sources"/> says, per field, where the new value came from; fields not listed were typed.
        /// Saved by the caller's SaveChanges.
        /// </summary>
        public async Task RecordEditsAsync(
            int submissionId,
            Actor actor,
            IReadOnlyDictionary<string, string> before,
            IReadOnlyDictionary<string, string> after,
            IReadOnlyDictionary<string, string> sources)
        {
            var changed = before.Keys.Union(after.Keys)
                .Where(k => Get(before, k) != Get(after, k))
                .ToList();
            if (changed.Count == 0) return;

            var now = DateTime.UtcNow;
            var since = now - MergeWindow;

            // The latest row for each changed field: candidate for merging. One submission has at most a few
            // hundred rows per field set, so picking the latest per field is done here rather than in SQL.
            var rows = await _db.SubmissionChanges
                .Where(c => c.SubmissionId == submissionId && c.FieldPath != null && changed.Contains(c.FieldPath))
                .ToListAsync();
            var latestByPath = rows
                .GroupBy(c => c.FieldPath!, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.Id).First(), StringComparer.Ordinal);

            foreach (var path in changed)
            {
                var oldValue = Get(before, path);
                var newValue = Get(after, path);
                var source = sources.TryGetValue(path, out var s) ? s : ValueSource.User;

                if (latestByPath.TryGetValue(path, out var last)
                    && last.Kind == SubmissionChangeKind.Edited
                    && last.UserId == actor.UserId
                    && last.Source == source
                    && last.At >= since
                    && last.NewValue == oldValue)
                {
                    // Same person still working on the same field: extend that change
                    if (last.OldValue == newValue)
                    {
                        _db.SubmissionChanges.Remove(last);   // back to where it started: no change at all
                    }
                    else
                    {
                        last.NewValue = newValue;
                        last.At = now;
                    }
                    continue;
                }

                _db.SubmissionChanges.Add(new SubmissionChange
                {
                    SubmissionId = submissionId,
                    At = now,
                    UserId = actor.UserId,
                    UserEmail = actor.Email,
                    Kind = SubmissionChangeKind.Edited,
                    FieldPath = path,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Source = source,
                });
            }
        }

        /// <summary>
        /// When a never-submitted draft is deleted: its field history and assistant conversation are removed
        /// with it (they hold the same data as the draft), and only a "Deleted" event is kept.
        /// The removal runs immediately (bulk delete); the "Deleted" event is saved by the caller's SaveChanges.
        /// </summary>
        public async Task ForgetDraftContentAsync(int submissionId, Actor actor)
        {
            await _db.SubmissionChanges.Where(c => c.SubmissionId == submissionId).ExecuteDeleteAsync();
            await _db.AssistantMessages.Where(m => m.SubmissionId == submissionId).ExecuteDeleteAsync();
            AddEvent(submissionId, actor, SubmissionChangeKind.Deleted);
        }

        // ============================================================ reading

        /// <summary>The whole history of a submission, newest first.</summary>
        public Task<List<SubmissionChange>> ListAsync(int submissionId, int max = 500) =>
            _db.SubmissionChanges.AsNoTracking()
                .Where(c => c.SubmissionId == submissionId)
                .OrderByDescending(c => c.Id)
                .Take(max)
                .ToListAsync();

        /// <summary>
        /// For each answer whose latest change came from the assistant or the Excel import (and was not
        /// typed over since): field path → source. Used to show the "please check" marks on the form
        /// and on the reviewer's page.
        /// </summary>
        public async Task<Dictionary<string, string>> UnconfirmedSourcesAsync(int submissionId)
        {
            var rows = await _db.SubmissionChanges.AsNoTracking()
                .Where(c => c.SubmissionId == submissionId && c.Kind == SubmissionChangeKind.Edited && c.FieldPath != null)
                .Select(c => new { c.Id, c.FieldPath, c.Source, c.NewValue })
                .ToListAsync();

            return rows
                .GroupBy(r => r.FieldPath!)
                .Select(g => g.OrderByDescending(r => r.Id).First())
                .Where(r => r.Source is ValueSource.Assistant or ValueSource.Import && r.NewValue != null)
                .ToDictionary(r => r.FieldPath!, r => r.Source!, StringComparer.Ordinal);
        }

        // ============================================================ helpers

        private static string? Get(IReadOnlyDictionary<string, string> d, string key) =>
            d.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;

        private static string? Cut(string? s) =>
            s == null ? null : s.Length > ContributionForm.MaxValueLength ? s[..ContributionForm.MaxValueLength] : s;
    }
}
