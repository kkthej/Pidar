namespace Pidar.Infrastructure;

/// <summary>
/// Application roles.
///   Admin   — everything: create, edit, delete datasets, manage users, Hangfire, ontology admin.
///   Curator — create and edit datasets only.
/// Every account must have one of these roles: an account without a role cannot sign in
/// (PIDAR is public to read; logging in is only for the team that maintains the data).
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Curator = "Curator";

    /// <summary>For [Authorize(Roles = ...)] on create/edit actions.</summary>
    public const string Editors = Admin + "," + Curator;

    public static readonly string[] All = { Admin, Curator };

    public static bool IsValid(string? role) => role is Admin or Curator;

    /// <summary>True when the signed-in user has the Admin or Curator role.</summary>
    public static bool HasAppRole(System.Security.Claims.ClaimsPrincipal? user) =>
        user != null && (user.IsInRole(Admin) || user.IsInRole(Curator));
}
