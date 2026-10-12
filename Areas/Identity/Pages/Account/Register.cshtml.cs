// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Pidar.Areas.Identity.Data;
using Pidar.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

namespace Pidar.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Self-registration for researchers who want to contribute datasets (Contribute page).
    /// New accounts get the DataProvider role, which only allows filling in and submitting their own
    /// contributions; the email address must be confirmed before the first login
    /// (RequireConfirmedAccount = true in Program.cs). Admin / Curator accounts are still created
    /// by an Admin on the Users page.
    /// </summary>
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<PidarUser> _signInManager;
        private readonly UserManager<PidarUser> _userManager;
        private readonly IUserStore<PidarUser> _userStore;
        private readonly IUserEmailStore<PidarUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<PidarUser> userManager,
            IUserStore<PidarUser> userStore,
            SignInManager<PidarUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            /// <summary>
            /// Honeypot: hidden from people with CSS, but simple bots fill in every field.
            /// When it has a value the request is answered as if it worked, and nothing is created.
            /// </summary>
            public string Website { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            // After confirming the email the user logs in and lands on the contribution page
            returnUrl ??= Url.Action("Index", "Submissions", new { area = "" }) ?? Url.Content("~/");
            ReturnUrl = returnUrl;

            if (!string.IsNullOrEmpty(Input?.Website))
            {
                _logger.LogWarning("Registration honeypot filled for {Email}; ignored", Input.Email);
                return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
            }

            if (!ModelState.IsValid)
                return Page();

            var email = Input.Email.Trim();
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                // Don't reveal which emails have accounts; point to the login / forgotten password pages
                ModelState.AddModelError(string.Empty,
                    "This email address cannot be registered. If you already have an account, log in or use \"Forgot your password?\".");
                return Page();
            }

            var user = CreateUser();
            await _userStore.SetUserNameAsync(user, email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, email, CancellationToken.None);
            user.LockoutEnabled = true;
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }

            var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.DataProvider);
            if (!roleResult.Succeeded)
            {
                // Without a role the account could never sign in: undo and report
                await _userManager.DeleteAsync(user);
                _logger.LogError("Could not add {Email} to {Role}: {Errors}", email, AppRoles.DataProvider,
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                ModelState.AddModelError(string.Empty, "The account could not be created. Please try again later or contact the PIDAR team.");
                return Page();
            }

            _logger.LogInformation("Data provider {Email} registered", email);

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId, code, returnUrl },
                protocol: Request.Scheme);

            try
            {
                await _emailSender.SendEmailAsync(email, "Confirm your PIDAR account",
                    "<p>Thank you for registering on PIDAR (Preclinical Image DAtaset Repository) to contribute a dataset.</p>" +
                    $"<p>Please confirm your email address: <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>confirm my account</a>.</p>" +
                    "<p>After confirming, log in and click \"Make your data FAIR\" on the Contribute page.</p>" +
                    "<p>If you did not register, you can ignore this email.</p>");
            }
            catch (Exception ex)
            {
                // The account exists; the user can ask for a new link on the confirmation page
                _logger.LogError(ex, "Could not send confirmation email to {Email}", email);
            }

            return RedirectToPage("RegisterConfirmation", new { email, returnUrl });
        }

        private PidarUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<PidarUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(PidarUser)}'. " +
                    $"Ensure that '{nameof(PidarUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<PidarUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<PidarUser>)_userStore;
        }
    }
}
