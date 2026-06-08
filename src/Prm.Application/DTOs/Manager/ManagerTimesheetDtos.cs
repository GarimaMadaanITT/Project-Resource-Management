namespace Prm.Application.DTOs.Manager;

public record TeamTimesheetRowDto(
    string EmployeeName,
    string ProjectName,
    decimal Hours,
    string Status);

public record TeamTimesheetsResponse(
    DateOnly WeekStart,
    IReadOnlyList<TeamTimesheetRowDto> Rows);

public record ManagerTimesheetEntryDetailDto(
    string ProjectName,
    decimal Hours,
    IReadOnlyList<string> ActivityTags);

public record ManagerEmployeeTimesheetDetailResponse(
    int EmployeeId,
    string EmployeeName,
    DateOnly WeekStart,
    string Status,
    IReadOnlyList<ManagerTimesheetEntryDetailDto> Entries);
