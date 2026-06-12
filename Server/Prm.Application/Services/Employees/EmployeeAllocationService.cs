using Prm.Application.Common;
using Prm.Application.DTOs.Employee;
using Prm.Application.Interfaces;
using Prm.Application.Validation;

namespace Prm.Application.Services.Employees;

public class EmployeeAllocationService : IEmployeeAllocationService
{
    private readonly IEmployeeContextService _context;
    private readonly IResourceProfileRepository _resourceProfiles;

    public EmployeeAllocationService(
        IEmployeeContextService context,
        IResourceProfileRepository resourceProfiles)
    {
        _context = context;
        _resourceProfiles = resourceProfiles;
    }

    public async Task<MyAllocationsResponse> GetMyAllocationsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var employeeContext = await _context.ResolveAsync(userId, cancellationToken);
        var activeAllocations = await _resourceProfiles.GetActiveAllocationsAsync(
            employeeContext.ResourceProfileId,
            cancellationToken);

        var items = activeAllocations
            .OrderBy(allocation => allocation.Project.Name)
            .Select(allocation => new EmployeeAllocationItemDto(
                allocation.ProjectId,
                allocation.Project.Name,
                allocation.UtilisationPercent,
                allocation.FromDate,
                allocation.ToDate,
                "Active"))
            .ToList();

        var totalUtilisation = ActiveDateHelper.SumActiveUtilisation(
            activeAllocations,
            ActiveDateHelper.TodayUtc);

        return new MyAllocationsResponse(items, totalUtilisation);
    }
}
