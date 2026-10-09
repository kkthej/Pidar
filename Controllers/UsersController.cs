using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Pidar.Areas.Identity.Data;
using Pidar.Infrastructure;

namespace Pidar.Controllers;

/// <summary>
/// Admin-only user management: list users, create accounts, set role
/// (Curator / Admin), deactivate / activate, send a password-reset link, delete.
/// Self-registration is disabled, so this is the only way to add users.
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
[Route("Users")]
public sealed class UsersController : Controller
{
    private readonly UserManager<PidarUser> _users;
    private readonly IEmailSender _email;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserManager<PidarUser> users, IEmailSender email, ILogger<UsersController> logger)
    {
        _users = users;
        _email = email;
        _logger = logger;
    }

    public sealed record UserRow(string Id, string Email, bool EmailConfirmed, bool IsActive, string Role, bool IsSelf);

    // GET /Users
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var me = _users.GetUserId(User);
        var list = await _users.Users.OrderBy(u => u.Email).ToListAsync();

        var rows = new List<UserRow>();
        foreach (var u in list)
        {
            var roles = await _users.GetRolesAsync(u);
            var role = roles.Contains(AppRoles.Admin) ? AppRoles.Admin
                     : roles.Contains(AppRoles.Curator) ? AppRoles.Curator
                     : "";   // older account without a role: cannot sign in until one is given
            var active = u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow;
            rows.Add(new UserRow(u.Id, u.Email ?? u.UserName ?? "", u.EmailConfirmed, active, role, u.Id == me));
        }

        return View(rows);
    }

    // POST /Users/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string email, string role)
    {
        email = (email ?? "").Trim();
        if (!AppRoles.IsValid(role))
        {
            TempData["Error"] = "Choose a role: Curator or Admin.";
            return RedirectToAction(nameof(Index));
        }
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            TempData["Error"] = "Please enter a valid email address.";
            return RedirectToAction(nameof(Index));
        }
        if (await _users.FindByEmailAsync(email) != null)
        {
            TempData["Error"] = $"An account for {email} already exists.";
            return RedirectToAction(nameof(Index));
        }

        // Admin-created accounts are trusted: email marked confirmed, no password yet.
        var user = new PidarUser { UserName = email, Email = email, EmailConfirmed = true, LockoutEnabled = true };
        var result = await _users.CreateAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        await _users.AddToRoleAsync(user, role);

        _logger.LogInformation("{Admin} created user {Email} with role {Role}", User.Identity?.Name, email, role);
        await SendSetPasswordLink(user, isNewAccount: true);
        return RedirectToAction(nameof(Index));
    }

    // POST /Users/SetRole
    [HttpPost("SetRole")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (!AppRoles.IsValid(role))
        {
            TempData["Error"] = "Choose a role: Curator or Admin. To remove someone's access, deactivate or delete the account.";
            return RedirectToAction(nameof(Index));
        }

        if (user.Id == _users.GetUserId(User) && role != AppRoles.Admin)
        {
            TempData["Error"] = "You can't remove your own Admin role.";
            return RedirectToAction(nameof(Index));
        }

        var current = await _users.GetRolesAsync(user);
        var toRemove = current.Where(r => AppRoles.All.Contains(r)).ToList();
        if (toRemove.Any()) await _users.RemoveFromRolesAsync(user, toRemove);
        await _users.AddToRoleAsync(user, role);

        // Forces the user's existing login to refresh (picks up the new role / gets signed out)
        await _users.UpdateSecurityStampAsync(user);

        _logger.LogInformation("{Admin} set role of {Email} to {Role}", User.Identity?.Name, user.Email, role);
        TempData["Message"] = $"{user.Email} is now: {role}.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Users/SetActive
    [HttpPost("SetActive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(string id, bool active)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (user.Id == _users.GetUserId(User) && !active)
        {
            TempData["Error"] = "You can't deactivate your own account.";
            return RedirectToAction(nameof(Index));
        }

        await _users.SetLockoutEnabledAsync(user, true);
        await _users.SetLockoutEndDateAsync(user, active ? null : DateTimeOffset.MaxValue);
        await _users.UpdateSecurityStampAsync(user);

        _logger.LogInformation("{Admin} set {Email} active={Active}", User.Identity?.Name, user.Email, active);
        TempData["Message"] = $"{user.Email} is now {(active ? "active" : "deactivated")}.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Users/SendReset
    [HttpPost("SendReset")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendReset(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return NotFound();
        await SendSetPasswordLink(user, isNewAccount: false);
        return RedirectToAction(nameof(Index));
    }

    // POST /Users/Delete
    [HttpPost("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (user.Id == _users.GetUserId(User))
        {
            TempData["Error"] = "You can't delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _users.DeleteAsync(user);
        _logger.LogInformation("{Admin} deleted user {Email}: {Ok}", User.Identity?.Name, user.Email, result.Succeeded);
        TempData[result.Succeeded ? "Message" : "Error"] = result.Succeeded
            ? $"{user.Email} was deleted."
            : string.Join(" ", result.Errors.Select(e => e.Description));
        return RedirectToAction(nameof(Index));
    }

    // Emails a "set your password" link (Identity's ResetPassword page).
    // The link is also shown to the admin, so it can be passed on if email fails.
    private async Task SendSetPasswordLink(PidarUser user, bool isNewAccount)
    {
        var code = await _users.GeneratePasswordResetTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var url = Url.Page("/Account/ResetPassword", pageHandler: null,
            values: new { area = "Identity", code }, protocol: Request.Scheme)!;

        var subject = isNewAccount ? "Your PIDAR account" : "Reset your PIDAR password";
        var intro = isNewAccount
            ? "An account has been created for you on PIDAR (Preclinical Image DAtaset Repository)."
            : "A password reset was requested for your PIDAR account.";
        var body = $"<p>{intro}</p><p>Set your password here: <a href='{HtmlEncoder.Default.Encode(url)}'>set password</a></p>" +
                   $"<p>On that page enter this email address ({HtmlEncoder.Default.Encode(user.Email ?? "")}) and choose a password.</p>";

        if (_email is Pidar.Services.Email.SmtpEmailSender smtp && !smtp.IsConfigured)
        {
            TempData["Error"] = $"Email is not configured on this server, so nothing was sent to {user.Email}. Give them the link below.";
        }
        else try
        {
            await _email.SendEmailAsync(user.Email!, subject, body);
            TempData["Message"] = $"Password link sent to {user.Email}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not email password link to {Email}", user.Email);
            TempData["Error"] = $"Could not send the email to {user.Email} (check the SMTP settings). You can give them the link below instead.";
        }
        TempData["Link"] = url;
        TempData["LinkFor"] = user.Email;
    }
}
