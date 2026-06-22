using Prm.Application.Common;
using Prm.Application.DTOs.Employee;
using Prm.Application.Interfaces;
using Prm.Application.Validation;

namespace Prm.Application.Services.Employees;

public class EmployeeAllocationService : IEmployeeAllocationService
{
    private readonly IEmployeeContextService _context;
    private readonly IEmployeeRepository _employees;

    public EmployeeAllocationService(
        IEmployeeContextService context,
        IEmployeeRepository employees)
    {
        _context = context;
        _employees = employees;
    }

    public async Task<MyAllocationsResponse> GetMyAllocationsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var employeeContext = await _context.ResolveAsync(userId, cancellationToken);
        var activeAllocations = await _employees.GetActiveAllocationsAsync(
            employeeContext.EmployeeId,
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
