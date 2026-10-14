namespace Pidar.Models.Submissions
{
    /// <summary>
    /// One line of a submission's history: who did what, and when.
    ///
    /// Two kinds of rows:
    ///   - events (Created, Submitted, Approved, …): FieldPath is null, Note may hold details
    ///     (reviewer comment, created dataset id);
    ///   - field edits (Kind = Edited): one row per changed answer, with the old and new value and
    ///     where the new value came from (<see cref="ValueSource"/>).
    ///
    /// Rows are only ever added (or merged while the same person keeps typing in the same field,
    /// see SubmissionHistory), never edited afterwards. There is no foreign key to dataset_submission,
    /// so the "Deleted" event of a draft outlives the draft itself.
    /// </summary>
    public class SubmissionChange
    {
        public long Id { get; set; }
        public int SubmissionId { get; set; }

        /// <summary>UTC.</summary>
        public DateTime At { get; set; }

        /// <summary>Identity user id of who made the change ("system" for automatic changes).</summary>
        public string UserId { get; set; } = "";

        /// <summary>Their email at that moment, so the history stays readable if the account is renamed or removed.</summary>
        public string UserEmail { get; set; } = "";

        /// <summary>One of <see cref="SubmissionChangeKind"/>.</summary>
        public string Kind { get; set; } = SubmissionChangeKind.Edited;

        /// <summary>"Section.Property" of the edited answer; null for events.</summary>
        public string? FieldPath { get; set; }

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        /// <summary>For edits: one of <see cref="ValueSource"/>.</summary>
        public string? Source { get; set; }

        /// <summary>Free text for events: the reviewer's message, "dataset 42", "copy of submission 7", …</summary>
        public string? Note { get; set; }
    }

    public static class SubmissionChangeKind
    {
        public const string Created = "Created";
        public const string Edited = "Edited";
        public const string Submitted = "Submitted";
        public const string ChangesRequested = "ChangesRequested";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Deleted = "Deleted";

        public static string Label(string kind) => kind switch
        {
            Created => "Started the description",
            Edited => "Edited",
            Submitted => "Submitted for review",
            ChangesRequested => "Asked for changes",
            Approved => "Approved and published",
            Rejected => "Rejected",
            Deleted => "Deleted the draft",
            _ => kind
        };

        public static string Icon(string kind) => kind switch
        {
            Created => "bi-plus-circle",
            Edited => "bi-pencil",
            Submitted => "bi-send",
            ChangesRequested => "bi-arrow-return-left",
            Approved => "bi-check2-circle",
            Rejected => "bi-x-circle",
            Deleted => "bi-trash",
            _ => "bi-dot"
        };
    }

    /// <summary>Where an answer came from.</summary>
    public static class ValueSource
    {
        /// <summary>Typed or clicked by the person.</summary>
        public const string User = "user";
        /// <summary>Suggested by the contribution assistant and accepted by the person, unchanged since.</summary>
        public const string Assistant = "assistant";
        /// <summary>Filled from the Excel metadata template, unchanged since.</summary>
        public const string Import = "import";
        /// <summary>Set by PIDAR itself (e.g. defaults of a new draft).</summary>
        public const string System = "system";

        /// <summary>Sources a browser may claim when saving; anything else counts as <see cref="User"/>.</summary>
        public static bool IsClientClaimable(string? s) => s is Assistant or Import;

        public static string Label(string? s) => s switch
        {
            Assistant => "suggested by the assistant",
            Import => "from the Excel template",
            System => "automatic",
            _ => "typed"
        };
    }
}
