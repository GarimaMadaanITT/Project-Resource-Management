namespace Prm.Application.DTOs.Employee;

public record ActivityTagsResponse(
    IReadOnlyList<string> PredefinedTags,
    bool AllowsCustomOther);

public record SubmitTimesheetEntryRequest(
    int ProjectId,
    decimal Hours,
    IReadOnlyList<string> ActivityTags);

public record SubmitTimesheetRequest(
    DateOnly? WeekStart,
    IReadOnlyList<SubmitTimesheetEntryRequest> Entries);

public record SubmitTimesheetResponse(
    DateOnly WeekStart,
    decimal TotalHours,
    string Status,
    string Message);

public record TimesheetListItemDto(
    DateOnly WeekStart,
    decimal TotalHours,
    string Status);

public record MyTimesheetsResponse(IReadOnlyList<TimesheetListItemDto> Weeks);

public record TimesheetEntryDetailDto(
    string ProjectName,
    decimal Hours,
    IReadOnlyList<string> ActivityTags);

public record TimesheetWeekDetailDto(
    DateOnly WeekStart,
    decimal TotalHours,
    string Status,
    IReadOnlyList<TimesheetEntryDetailDto> Entries);
