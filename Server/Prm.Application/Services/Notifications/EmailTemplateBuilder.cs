using Prm.Domain.Enums;

namespace Prm.Application.Services.Notifications;

public static class EmailTemplateBuilder
{
    public static string BuildTimesheetReminder1(string employeeName, DateOnly weekStart) =>
        $"""
        Dear {employeeName},

        This is a reminder that your timesheet for the week starting {weekStart:dd-MMM-yyyy} has not been submitted.
        The submission deadline was Friday {weekStart.AddDays(4):dd-MMM-yyyy}.

        Please submit your timesheet as soon as possible to remain compliant.

        Regards,
        PRM System
        """;

    public static string BuildTimesheetReminder2(string employeeName, DateOnly weekStart) =>
        $"""
        Dear {employeeName},

        This is your second reminder. Your timesheet for the week starting {weekStart:dd-MMM-yyyy} is still pending.
        If it is not submitted by end of day Tuesday, your timesheet submission access may be restricted.

        Please submit immediately.

        Regards,
        PRM System
        """;

    public static string BuildTimesheetFrozenEmployee(string employeeName, DateOnly weekStart) =>
        $"""
        Dear {employeeName},

        Your timesheet for the week starting {weekStart:dd-MMM-yyyy} was not submitted after two reminders.
        Your timesheet submission access has been frozen.

        You can still log in and view your dashboard, but you cannot create, update, or submit timesheets until your manager restores access.

        Regards,
        PRM System
        """;

    public static string BuildTimesheetFrozenManager(string managerName, string employeeName, DateOnly weekStart) =>
        $"""
        Dear {managerName},

        {employeeName} did not submit a timesheet for the week starting {weekStart:dd-MMM-yyyy} after two reminders.
        Their timesheet submission access has been frozen.

        Please review and restore access from the manager console when appropriate.

        Regards,
        PRM System
        """;

    public static string BuildProjectAtRisk(
        string projectName,
        string managerName,
        HealthStatus healthStatus,
        IReadOnlyList<string> milestoneLines,
        string aiSummary,
        IReadOnlyList<string> suggestedEmployees)
    {
        var healthLabel = healthStatus switch
        {
            HealthStatus.OnTrack => "Green (On Track)",
            HealthStatus.Attention => "Amber (Attention)",
            HealthStatus.AtRisk => "Red (At Risk)",
            _ => healthStatus.ToString()
        };

        var milestones = milestoneLines.Count == 0
            ? "  (none)"
            : string.Join(Environment.NewLine, milestoneLines.Select(line => $"  - {line}"));

        var suggestions = suggestedEmployees.Count == 0
            ? "  (none available)"
            : string.Join(Environment.NewLine, suggestedEmployees.Select(line => $"  - {line}"));

        return $"""
        Dear {managerName},

        Project "{projectName}" has been marked AT RISK by the Project Health Scheduler.

        HEALTH STATUS: {healthLabel}

        PROJECT MILESTONES:
        {milestones}

        AI RISK SUMMARY:
        {aiSummary}

        SUGGESTED HELP (available employees):
        {suggestions}

        Please review the project dashboard and take action promptly.

        Regards,
        PRM System
        """;
    }

    public static string Reminder1Subject(DateOnly weekStart) =>
        $"Timesheet Reminder 1 — week {weekStart:dd-MMM-yyyy}";

    public static string Reminder2Subject(DateOnly weekStart) =>
        $"Timesheet Reminder 2 — week {weekStart:dd-MMM-yyyy}";

    public static string FrozenEmployeeSubject() =>
        "Timesheet Access Frozen";

    public static string FrozenManagerSubject(string employeeName) =>
        $"Timesheet Access Frozen — {employeeName}";

    public static string ProjectAtRiskSubject(string projectName) =>
        $"Project At Risk — {projectName}";
}
