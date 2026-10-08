using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Pidar.Areas.Identity.Data;

namespace Pidar.Services
{
    /// <summary>
    /// Remembers when each user last signed in, for the "Last login" shown in the session bar.
    ///
    /// The times are kept in Identity's user_tokens table (provider "Pidar") rather than in new columns
    /// on users, because the Identity tables have no EF Core migrations in this project: no schema
    /// change is needed and nothing has to be migrated on the server.
    ///
    /// Two values per user: the current sign-in and the one before it. The bar shows the one before,
    /// like XNAT, so a user can spot a sign-in that wasn't theirs.
    /// </summary>
    public static class LastLoginStore
    {
        private const string Provider = "Pidar";
        private const string CurrentKey = "LastLoginUtc";
        private const string PreviousKey = "PreviousLoginUtc";

        /// <summary>
        /// Call once after a successful interactive sign-in. Never throws: failing to record the time
        /// must not stop the user from signing in.
        /// </summary>
        public static async Task RecordAsync(UserManager<PidarUser> userManager, PidarUser? user, ILogger logger)
        {
            if (user is null) return;
            try
            {
                var current = await userManager.GetAuthenticationTokenAsync(user, Provider, CurrentKey);
                if (!string.IsNullOrEmpty(current))
                    await userManager.SetAuthenticationTokenAsync(user, Provider, PreviousKey, current);

                await userManager.SetAuthenticationTokenAsync(user, Provider, CurrentKey,
                    DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not record the sign-in time for user {UserId}", user.Id);
            }
        }

        /// <summary>
        /// The sign-in before the current one, or the current one on a user's first sign-in.
        /// Null for users who haven't signed in since this feature was deployed.
        /// </summary>
        public static async Task<DateTime?> GetLastLoginUtcAsync(UserManager<PidarUser> userManager, PidarUser user)
        {
            var value = await userManager.GetAuthenticationTokenAsync(user, Provider, PreviousKey)
                        ?? await userManager.GetAuthenticationTokenAsync(user, Provider, CurrentKey);

            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var utc)
                ? utc
                : null;
        }
    }
}
