using Prm.Application.DTOs.Manager;

namespace Prm.Application.Interfaces;

public interface IManagerTeamTimesheetService
{
    Task<TeamTimesheetsResponse> GetTeamTimesheetsAsync(
        int managerUserId,
        DateOnly? weekStart,
        CancellationToken cancellationToken = default);
    Task<ManagerEmployeeTimesheetDetailResponse> GetEmployeeTimesheetDetailAsync(
        int managerUserId,
        int employeeId,
        DateOnly? weekStart,
        CancellationToken cancellationToken = default);
}
