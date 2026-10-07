using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace Pidar.Services.Email;

/// <summary>
/// SMTP settings, bound from configuration section "Smtp".
/// In Docker set them as environment variables, e.g. Smtp__Host, Smtp__User, Smtp__Password.
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

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(User) &&
        !string.IsNullOrWhiteSpace(Password);
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
            _logger.LogWarning("SMTP is not configured (Smtp__Host/User/Password). Email to {Email} with subject '{Subject}' was NOT sent.",
                email, subject);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.From ?? _options.User!, _options.FromName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        message.To.Add(email);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.User, _options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

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
}
