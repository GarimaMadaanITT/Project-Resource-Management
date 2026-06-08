using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Entities;

namespace Prm.Application.Services.Manager;

public class ManagerDashboardService : IManagerDashboardService
{
    private readonly IManagerContextService _context;
    private readonly IEmployeeRepository _employees;
    private readonly ITimesheetRepository _timesheets;

    public ManagerDashboardService(
        IManagerContextService context,
        IEmployeeRepository employees,
        ITimesheetRepository timesheets)
    {
        _context = context;
        _employees = employees;
        _timesheets = timesheets;
    }

    public async Task<ManagerDashboardResponse> GetDashboardAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        var managerContext = await _context.ResolveAsync(managerUserId, cancellationToken);
        var team = await _employees.GetTeamByManagerEmployeeIdAsync(
            managerContext.ManagerEmployeeId,
            activeOnly: true,
            cancellationToken);

        var today = ActiveDateHelper.TodayUtc;
        var bench = new List<ManagerDashboardEmployeeDto>();
        var partial = new List<ManagerDashboardEmployeeDto>();
        var full = new List<ManagerDashboardEmployeeDto>();

        foreach (var employee in team)
        {
            var utilisation = ActiveDateHelper.SumActiveUtilisation(employee.Allocations, today);
            var dto = MapEmployee(employee, utilisation);

            if (utilisation == 0)
            {
                bench.Add(dto);
            }
            else if (utilisation >= ValidationConstants.MaxUtilisationPercent)
            {
                full.Add(dto);
            }
            else
            {
                partial.Add(dto);
            }
        }

        return new ManagerDashboardResponse(
            bench,
            partial,
            full,
            bench.Count,
            partial.Count,
            full.Count);
    }

    public async Task<ManagerEmployeeDetailDto> GetEmployeeDetailAsync(
        int managerUserId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var managerContext = await _context.ResolveAsync(managerUserId, cancellationToken);
        var employee = EntityGuard.EnsureFound(
            await _employees.GetTeamMemberAsync(managerContext.ManagerEmployeeId, employeeId, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        var today = ActiveDateHelper.TodayUtc;
        var utilisation = ActiveDateHelper.SumActiveUtilisation(employee.Allocations, today);
        var activeAllocations = employee.Allocations
            .Where(allocation => ActiveDateHelper.IsAllocationActive(allocation, today))
            .Select(allocation => new ManagerAllocationItemDto(
                allocation.Project.Name,
                allocation.UtilisationPercent,
                allocation.FromDate,
                allocation.ToDate))
            .ToList();

        var recentTags = await _timesheets.GetRecentActivityTagsAsync(
            employee.Id,
            ValidationConstants.DefaultRecentActivityWeeks,
            cancellationToken);

        return new ManagerEmployeeDetailDto(
            employee.Id,
            employee.User.FullName,
            employee.Department,
            EmployeeStatusResolver.ResolveStatusName(utilisation),
            utilisation,
            employee.Skills.Select(skill => skill.Skill.Name).ToList(),
            activeAllocations,
            recentTags);
    }

    private static ManagerDashboardEmployeeDto MapEmployee(Employee employee, int utilisation)
    {
        var skills = string.Join(", ", employee.Skills.Select(skill => skill.Skill.Name));
        var availability = ValidationConstants.MaxUtilisationPercent - utilisation;

        return new ManagerDashboardEmployeeDto(
            employee.Id,
            employee.User.FullName,
            employee.Department,
            utilisation,
            availability,
            skills);
    }
}
