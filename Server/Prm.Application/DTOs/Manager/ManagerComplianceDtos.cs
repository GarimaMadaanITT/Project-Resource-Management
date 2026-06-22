namespace Prm.Application.DTOs.Manager;

public record RestoreTimesheetAccessResponse(
    int EmployeeId,
    string EmployeeName,
    bool TimesheetSubmissionFrozen,
    string Message);
