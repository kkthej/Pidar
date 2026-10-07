namespace Pidar.Infrastructure;

/// <summary>
/// Application roles.
///   Admin   — everything: create, edit, delete datasets, manage users, Hangfire, ontology admin.
///   Curator — create and edit datasets only.
///   (no role) — can log in, but has no edit rights.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Curator = "Curator";

    /// <summary>For [Authorize(Roles = ...)] on create/edit actions.</summary>
    public const string Editors = Admin + "," + Curator;

    public static readonly string[] All = { Admin, Curator };
}
