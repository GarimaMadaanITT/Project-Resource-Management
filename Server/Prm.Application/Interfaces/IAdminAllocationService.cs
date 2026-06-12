using Prm.Application.DTOs.Admin;

namespace Prm.Application.Interfaces;

public interface IAdminAllocationService
{
    Task<IReadOnlyList<AllocationListItemDto>> GetAllAsync(int? employeeId, int? projectId, CancellationToken cancellationToken = default);
}
