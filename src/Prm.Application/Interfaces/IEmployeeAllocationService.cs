using Prm.Application.DTOs.Employee;

namespace Prm.Application.Interfaces;

public interface IEmployeeAllocationService
{
    Task<MyAllocationsResponse> GetMyAllocationsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
