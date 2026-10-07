using Microsoft.AspNetCore.Identity;
using Pidar.Areas.Identity.Data;

namespace Pidar.Infrastructure;

/// <summary>
/// Runs at startup: makes sure the Admin and Curator roles exist, and that the
/// configured admin accounts have the Admin role.
///
/// Admin accounts come from configuration "Identity:AdminEmails" (comma-separated),
/// e.g. environment variable Identity__AdminEmails=admin@pidar.hpc4ai.unito.it,you@unito.it
/// Default: admin@pidar.hpc4ai.unito.it. Accounts that don't exist are skipped (never created here).
/// </summary>
public static class IdentitySeeder
{
    private const string DefaultAdminEmail = "admin@pidar.hpc4ai.unito.it";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");
        var config = sp.GetRequiredService<IConfiguration>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<PidarUser>>();

        try
        {
            foreach (var role in AppRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Created role {Role}", role);
                }
            }

            var emails = (config["Identity:AdminEmails"] ?? DefaultAdminEmail)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var email in emails)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    logger.LogWarning("Admin seed: no account for {Email}, skipped", email);
                    continue;
                }
                if (!await userManager.IsInRoleAsync(user, AppRoles.Admin))
                {
                    await userManager.AddToRoleAsync(user, AppRoles.Admin);
                    logger.LogInformation("Admin seed: added {Email} to Admin", email);
                }
            }
        }
        catch (Exception ex)
        {
            // Never block startup because of seeding
            logger.LogError(ex, "Identity seeding failed");
        }
    }
}
