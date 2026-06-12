namespace Prm.Application.Common;

public static class ValidationConstants
{
    public const int MinPasswordLength = 8;
    public const int MinActiveAdminCount = 1;
    public const int MinJwtKeyLength = 32;
    public const int MinUtilisationPercent = 1;
    public const int MaxUtilisationPercent = 100;
    public const int DefaultRecentActivityWeeks = 4;
    public const int TimesheetHistoryWeeks = 12;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxTeamBuilderRequirementLength = 1000;
}
