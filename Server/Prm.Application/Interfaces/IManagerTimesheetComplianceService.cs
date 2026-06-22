using Prm.Application.DTOs.Manager;

namespace Prm.Application.Interfaces;

public interface IManagerTimesheetComplianceService
{
    Task<RestoreTimesheetAccessResponse> RestoreTimesheetAccessAsync(
        int managerUserId,
        int employeeId,
        CancellationToken cancellationToken = default);
}
