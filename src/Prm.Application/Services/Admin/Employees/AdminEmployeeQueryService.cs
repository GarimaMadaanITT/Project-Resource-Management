using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Admin.Employees;

public class AdminEmployeeQueryService
{
    private readonly IEmployeeRepository _employees;

    public AdminEmployeeQueryService(IEmployeeRepository employees)
    {
        _employees = employees;
    }

    public async Task<EmployeeListResponse> GetAllAsync(
        string? department,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var statusFilter = ParseEmployeeStatus(status);
        var employees = (await _employees.GetAllAsync(department, statusFilter, cancellationToken))
            .Where(employee => employee.IsActive)
            .ToList();

        var items = employees.Select(employee =>
        {
            var utilisation = ActiveDateHelper.SumActiveUtilisation(employee.Allocations, ActiveDateHelper.TodayUtc);
            return new EmployeeListItemDto(
                employee.Id,
                employee.User.FullName,
                employee.Department,
                EmployeeStatusResolver.ResolveStatusName(utilisation),
                employee.IsActive,
                utilisation);
        }).ToList();

        if (statusFilter.HasValue)
        {
            items = items.Where(item => item.Status == statusFilter.Value.ToString()).ToList();
        }

        return new EmployeeListResponse(
            items,
            items.Count,
            items.Count(item => item.Status == EmployeeStatus.Allocated.ToString()),
            items.Count(item => item.Status == EmployeeStatus.Bench.ToString()));
    }

    private static EmployeeStatus? ParseEmployeeStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) ? null : EnumGuard.Parse<EmployeeStatus>(status, "Status");
}
