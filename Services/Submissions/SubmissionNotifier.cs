using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Pidar.Areas.Identity.Data;
using Pidar.Infrastructure;
using Pidar.Models.Submissions;

namespace Pidar.Services.Submissions
{
    /// <summary>
    /// Emails about contributions. A failed email is logged and never stops the action itself
    /// (the submission is saved either way, and its status is always visible on the site).
    /// </summary>
    public sealed class SubmissionNotifier
    {
        private readonly IEmailSender _email;
        private readonly UserManager<PidarUser> _users;
        private readonly ILogger<SubmissionNotifier> _logger;

        public SubmissionNotifier(IEmailSender email, UserManager<PidarUser> users, ILogger<SubmissionNotifier> logger)
        {
            _email = email;
            _users = users;
            _logger = logger;
        }

        private static string E(string? s) => HtmlEncoder.Default.Encode(s ?? "");

        /// <summary>To the provider (receipt) and to every Admin and Curator (something to review).</summary>
        public async Task SubmittedAsync(DatasetSubmission s, string providerLink, string reviewLink)
        {
            var title = SubmissionService.Title(s);

            await SendAsync(s.OwnerEmail, "PIDAR: we received your dataset description",
                $"<p>Thank you! Your dataset description <strong>{E(title)}</strong> was submitted to PIDAR.</p>" +
                "<p>The PIDAR team will check it and either publish it or ask you for changes. You will get an email either way.</p>" +
                $"<p>You can see its status here: <a href='{E(providerLink)}'>my contributions</a>.</p>");

            var reviewers = new List<PidarUser>();
            reviewers.AddRange(await _users.GetUsersInRoleAsync(AppRoles.Admin));
            reviewers.AddRange(await _users.GetUsersInRoleAsync(AppRoles.Curator));

            foreach (var r in reviewers.Where(r => !string.IsNullOrWhiteSpace(r.Email))
                                       .GroupBy(r => r.Email!, StringComparer.OrdinalIgnoreCase).Select(g => g.First()))
            {
                await SendAsync(r.Email!, $"PIDAR: new contribution to review – {title}",
                    $"<p>{E(s.OwnerEmail)} submitted the dataset description <strong>{E(title)}</strong>.</p>" +
                    $"<p>Review it here: <a href='{E(reviewLink)}'>open the submission</a>.</p>");
            }
        }

        public Task ChangesRequestedAsync(DatasetSubmission s, string providerLink) =>
            SendAsync(s.OwnerEmail, "PIDAR: changes requested for your dataset description",
                $"<p>The PIDAR team reviewed <strong>{E(SubmissionService.Title(s))}</strong> and asks for some changes:</p>" +
                $"<blockquote style='border-left:3px solid #ccc;padding-left:10px;white-space:pre-wrap'>{E(s.ReviewerComment)}</blockquote>" +
                $"<p>Please update the form and submit it again: <a href='{E(providerLink)}'>open my dataset description</a>.</p>");

        public Task ApprovedAsync(DatasetSubmission s, string datasetLink) =>
            SendAsync(s.OwnerEmail, "PIDAR: your dataset is published",
                $"<p>Good news: <strong>{E(SubmissionService.Title(s))}</strong> has been approved and is now listed in PIDAR.</p>" +
                $"<p><a href='{E(datasetLink)}'>View the dataset</a></p>" +
                "<p>The PIDAR team may still add ontology codes to make it easier to find.</p>");

        public Task RejectedAsync(DatasetSubmission s, string providerLink) =>
            SendAsync(s.OwnerEmail, "PIDAR: your dataset description was not accepted",
                $"<p>The PIDAR team reviewed <strong>{E(SubmissionService.Title(s))}</strong> and could not accept it:</p>" +
                $"<blockquote style='border-left:3px solid #ccc;padding-left:10px;white-space:pre-wrap'>{E(s.ReviewerComment)}</blockquote>" +
                $"<p><a href='{E(providerLink)}'>My contributions</a></p>");

        private async Task SendAsync(string to, string subject, string html)
        {
            if (string.IsNullOrWhiteSpace(to)) return;
            try
            {
                await _email.SendEmailAsync(to, subject, html);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send '{Subject}' to {Email}", subject, to);
            }
        }
    }
}
