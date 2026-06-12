using Prm.Application.DTOs.Employee;

namespace Prm.Application.Interfaces;

public interface IEmployeeTimesheetService
{
    ActivityTagsResponse GetActivityTags();
    Task<SubmitTimesheetResponse> SubmitAsync(
        int userId,
        SubmitTimesheetRequest request,
        CancellationToken cancellationToken = default);
    Task<MyTimesheetsResponse> GetMyTimesheetsAsync(
        int userId,
        CancellationToken cancellationToken = default);
    Task<TimesheetWeekDetailDto> GetWeekDetailAsync(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
}
