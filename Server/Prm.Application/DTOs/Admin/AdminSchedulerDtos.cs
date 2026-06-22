namespace Prm.Application.DTOs.Admin;

public record SchedulerRunResponse(string Message);

public record NotificationTestDataSeedResponse(
    string Message,
    DateOnly PreviousWeekStart,
    IReadOnlyList<string> PreparedEmployees);

public record TimesheetComplianceForceResponse(
    string Username,
    bool Reminder1Sent,
    bool Reminder2Sent,
    bool Frozen,
    bool TimesheetSubmissionFrozen,
    string Message);
