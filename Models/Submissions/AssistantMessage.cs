namespace Pidar.Models.Submissions
{
    /// <summary>
    /// One message of the conversation between a data provider and the contribution assistant,
    /// kept with the submission so that:
    ///   - the conversation can be continued after closing the page (save and resume),
    ///   - a curator can see where a suggested answer came from,
    ///   - assistant usage (and cost: token counts) can be followed per user.
    ///
    /// Only the visible text is stored, plus the suggestions shown with an assistant reply.
    /// Deleted together with its draft (SubmissionsController.Delete).
    /// </summary>
    public class AssistantMessage
    {
        public long Id { get; set; }
        public int SubmissionId { get; set; }

        /// <summary>Owner of the conversation (the data provider).</summary>
        public string UserId { get; set; } = "";

        /// <summary>"user" or "assistant".</summary>
        public string Role { get; set; } = AssistantRole.User;

        public string Text { get; set; } = "";

        /// <summary>For assistant replies: the suggested answers shown with it, as JSON (jsonb). Null when none.</summary>
        public string? ProposalsJson { get; set; }

        /// <summary>For assistant replies: model that answered ("mock" in test mode).</summary>
        public string? Model { get; set; }

        /// <summary>For assistant replies: tokens used for the whole reply (all tool rounds).</summary>
        public int? InputTokens { get; set; }
        public int? OutputTokens { get; set; }

        /// <summary>UTC.</summary>
        public DateTime CreatedAt { get; set; }
    }

    public static class AssistantRole
    {
        public const string User = "user";
        public const string Assistant = "assistant";
    }
}
