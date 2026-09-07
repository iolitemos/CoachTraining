namespace CoachTraining.Api.Constants;

/// <summary>
/// Canonical business role names (requirement.md section 3). Used as both the
/// Role.Name master-data value and the JWT/claims role string so backend
/// [Authorize(Roles = ...)] checks and frontend role guards stay in sync.
/// </summary>
public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Coach = "Coach";
    public const string ManagementViewer = "ManagementViewer";

    public static readonly IReadOnlyList<string> All = [Administrator, Coach, ManagementViewer];
}
