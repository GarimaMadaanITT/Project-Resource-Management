using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Validation;

namespace Prm.Application.Services.Admin;

public class AdminAllocationService : IAdminAllocationService
{
    private readonly IAllocationRepository _allocations;

    public AdminAllocationService(IAllocationRepository allocations)
    {
        _allocations = allocations;
    }

    public async Task<IReadOnlyList<AllocationListItemDto>> GetAllAsync(
        int? employeeId,
        int? projectId,
        CancellationToken cancellationToken = default)
    {
        var today = ActiveDateHelper.TodayUtc;
        var items = await _allocations.GetAllAsync(employeeId, projectId, cancellationToken);

        return items
            .Where(allocation => ActiveDateHelper.IsAllocationActive(allocation, today))
            .Select(a => new AllocationListItemDto(
                a.Employee.User.FullName,
                a.Project.Name,
                a.UtilisationPercent,
                a.FromDate,
                a.ToDate))
            .ToList();
    }
}
