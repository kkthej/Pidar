using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace Pidar.Services.Email;

/// <summary>
/// SMTP settings, bound from configuration section "Smtp".
/// In Docker set them as environment variables, e.g. Smtp__Host, Smtp__User, Smtp__Password.
/// Institutional relay: Host + From (+ Port/EnableSsl); User/Password only if the relay requires login.
/// Gmail: Host=smtp.gmail.com, Port=587, User=the Gmail address, Password=a 16-character app password.
/// </summary>
public sealed class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? User { get; set; }
    public string? Password { get; set; }
    /// <summary>Sender address. For Gmail this must be the Gmail account (or a verified alias).</summary>
    public string? From { get; set; }
    public string FromName { get; set; } = "PIDAR";
    /// <summary>Optional Reply-To, e.g. a real inbox, so replies and bounces don't go to a no-reply address.</summary>
    public string? ReplyTo { get; set; }

    /// <summary>Host and a sender address are enough; User/Password are optional (internal relays often need no login).</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(From ?? User);

    public bool HasCredentials => !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Password);
}

/// <summary>
/// Real email sender for ASP.NET Identity (confirmation, password reset, Users page invitations).
/// If SMTP is not configured it logs a warning instead of throwing, so the site keeps working.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning("SMTP is not configured (Smtp__Host and Smtp__From). Email to {Email} with subject '{Subject}' was NOT sent.",
                email, subject);
            return;
        }

        var from = new MailAddress((_options.From ?? _options.User)!, _options.FromName);

        // Build a "normal looking" message: plain-text + HTML alternatives, Message-ID and
        // Reply-To. HTML-only mail with a link and no Message-ID is often dropped by Gmail.
        using var message = new MailMessage { From = from, Subject = subject };
        message.To.Add(email);
        message.Headers.Add("Message-ID", $"<{Guid.NewGuid():N}@{from.Host}>");
        if (!string.IsNullOrWhiteSpace(_options.ReplyTo))
            message.ReplyToList.Add(new MailAddress(_options.ReplyTo));

        var text = HtmlToText(htmlMessage);
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            text, System.Text.Encoding.UTF8, MediaTypeNames.Text.Plain));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            WrapHtml(htmlMessage), System.Text.Encoding.UTF8, MediaTypeNames.Text.Html));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        if (_options.HasCredentials)
            client.Credentials = new NetworkCredential(_options.User, _options.Password);

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {Email}: {Subject}", email, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}: {Subject}", email, subject);
            throw;
        }
    }

    // Plain-text version: links become "text: url", tags removed
    private static string HtmlToText(string html)
    {
        var t = System.Text.RegularExpressions.Regex.Replace(html,
            "<a\\s[^>]*href\\s*=\\s*['\"]([^'\"]+)['\"][^>]*>(.*?)</a>", "$2: $1",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        t = System.Text.RegularExpressions.Regex.Replace(t, "</p>|<br\\s*/?>", "\n\n",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        t = System.Text.RegularExpressions.Regex.Replace(t, "<[^>]+>", "");
        t = System.Net.WebUtility.HtmlDecode(t);
        return System.Text.RegularExpressions.Regex.Replace(t, "\n{3,}", "\n\n").Trim() + "\n";
    }

    private static string WrapHtml(string body) =>
        "<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head>" +
        "<body style=\"font-family:Arial,sans-serif;font-size:14px;color:#222\">" + body +
        "<p style=\"color:#888;font-size:12px\">PIDAR - Preclinical Image DAtaset Repository</p></body></html>";
}
