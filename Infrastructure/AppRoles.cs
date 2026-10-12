namespace Pidar.Infrastructure;

/// <summary>
/// Application roles.
///   Admin        — everything: create, edit, delete datasets, manage users, Hangfire, ontology admin,
///                  review contributions.
///   Curator      — create and edit datasets, review contributions.
///   DataProvider — researchers who registered themselves on the Contribute page. They can only fill in,
///                  save and submit their own contributions (Submissions); nothing reaches the public
///                  dataset list until an Admin or Curator approves it.
/// Every account must have one of these roles: an account without a role cannot sign in.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Curator = "Curator";
    public const string DataProvider = "DataProvider";

    /// <summary>For [Authorize(Roles = ...)] on create/edit actions and on reviewing contributions.</summary>
    public const string Editors = Admin + "," + Curator;

    public static readonly string[] All = { Admin, Curator, DataProvider };

    public static bool IsValid(string? role) => role is Admin or Curator or DataProvider;

    /// <summary>True when the signed-in user has any PIDAR role (Admin, Curator or DataProvider).</summary>
    public static bool HasAppRole(System.Security.Claims.ClaimsPrincipal? user) =>
        user != null && (user.IsInRole(Admin) || user.IsInRole(Curator) || user.IsInRole(DataProvider));

    /// <summary>True for Admin and Curator: the PIDAR team that edits datasets and reviews contributions.</summary>
    public static bool IsEditor(System.Security.Claims.ClaimsPrincipal? user) =>
        user != null && (user.IsInRole(Admin) || user.IsInRole(Curator));

    /// <summary>Name shown on screen ("Data provider" rather than "DataProvider").</summary>
    public static string DisplayName(string? role) => role == DataProvider ? "Data provider" : role ?? "";
}
