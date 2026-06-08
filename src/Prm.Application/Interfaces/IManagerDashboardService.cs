using Prm.Application.DTOs.Manager;

namespace Prm.Application.Interfaces;

public interface IManagerDashboardService
{
    Task<ManagerDashboardResponse> GetDashboardAsync(int managerUserId, CancellationToken cancellationToken = default);
    Task<ManagerEmployeeDetailDto> GetEmployeeDetailAsync(
        int managerUserId,
        int employeeId,
        CancellationToken cancellationToken = default);
}
