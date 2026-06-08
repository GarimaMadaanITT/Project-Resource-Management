using Prm.Application.DTOs.Manager;

namespace Prm.Application.Interfaces;

public interface IManagerAllocationService
{
    Task<ManagerAllocationDto> CreateAsync(
        int managerUserId,
        CreateManagerAllocationRequest request,
        CancellationToken cancellationToken = default);
    Task<EndAllocationResponse> EndAsync(
        int managerUserId,
        int allocationId,
        EndAllocationRequest request,
        CancellationToken cancellationToken = default);
}
