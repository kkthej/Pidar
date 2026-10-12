namespace Pidar.Models.Submissions
{
    /// <summary>
    /// A dataset description filled in by a data provider on the Contribute page.
    ///
    /// Kept apart from the public dataset tables on purpose: a draft can be incomplete or wrong,
    /// and must never show up in the dataset list, search, statistics, exports or the XNAT sync.
    /// Only when an Admin or Curator approves it is a real Dataset created from it
    /// (SubmissionService.ApproveAsync), and ApprovedDatasetId points to that dataset.
    ///
    /// The answers are stored as JSON (PayloadJson): { "Publication.PaperTitle": "...", ... }, the same
    /// "Section.Property" names used by the Create form and the Excel prefill. A half-filled form is
    /// therefore always valid to save, and adding a field to the form needs no database change.
    /// </summary>
    public class DatasetSubmission
    {
        public int Id { get; set; }

        /// <summary>Identity user id of the data provider who owns the submission.</summary>
        public string OwnerUserId { get; set; } = "";

        /// <summary>Owner's email at creation time, for lists and notification emails.</summary>
        public string OwnerEmail { get; set; } = "";

        /// <summary>One of <see cref="SubmissionStatus"/>.</summary>
        public string Status { get; set; } = SubmissionStatus.Draft;

        /// <summary>Answers as a JSON object of "Section.Property" → text. Column type jsonb.</summary>
        public string PayloadJson { get; set; } = "{}";

        /// <summary>Last step of the form the provider was on, to reopen it there.</summary>
        public int CurrentStep { get; set; }

        /// <summary>Message from the reviewer when asking for changes or rejecting.</summary>
        public string? ReviewerComment { get; set; }

        /// <summary>Email of the Admin/Curator who last reviewed it.</summary>
        public string? ReviewedBy { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }

        /// <summary>The dataset created on approval (dataset.dataset_id). Not a foreign key, so deleting
        /// the dataset later does not touch the submission history.</summary>
        public int? ApprovedDatasetId { get; set; }
    }

    /// <summary>
    /// Draft → Submitted → (NeedsRevision → Submitted …) → Approved / Rejected.
    /// The provider can edit only Draft and NeedsRevision.
    /// </summary>
    public static class SubmissionStatus
    {
        public const string Draft = "Draft";
        public const string Submitted = "Submitted";
        public const string NeedsRevision = "NeedsRevision";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";

        public static bool IsEditableByOwner(string status) => status is Draft or NeedsRevision;

        public static string Label(string status) => status switch
        {
            Draft => "Draft",
            Submitted => "Submitted – waiting for review",
            NeedsRevision => "Changes requested",
            Approved => "Approved – published",
            Rejected => "Not accepted",
            _ => status
        };

        /// <summary>Bootstrap colour for the status badge.</summary>
        public static string Color(string status) => status switch
        {
            Draft => "secondary",
            Submitted => "primary",
            NeedsRevision => "warning",
            Approved => "success",
            Rejected => "danger",
            _ => "secondary"
        };
    }
}
