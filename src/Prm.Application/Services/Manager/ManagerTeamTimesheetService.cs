using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Manager;

public class ManagerTeamTimesheetService : IManagerTeamTimesheetService
{
    private readonly IManagerContextService _context;
    private readonly IEmployeeRepository _employees;
    private readonly ITimesheetRepository _timesheets;

    public ManagerTeamTimesheetService(
        IManagerContextService context,
        IEmployeeRepository employees,
        ITimesheetRepository timesheets)
    {
        _context = context;
        _employees = employees;
        _timesheets = timesheets;
    }

    public async Task<TeamTimesheetsResponse> GetTeamTimesheetsAsync(
        int managerUserId,
        DateOnly? weekStart,
        CancellationToken cancellationToken = default)
    {
        var managerContext = await _context.ResolveAsync(managerUserId, cancellationToken);
        var team = await _employees.GetTeamByManagerEmployeeIdAsync(
            managerContext.ManagerEmployeeId,
            activeOnly: true,
            cancellationToken);

        var resolvedWeekStart = weekStart ?? ActiveDateHelper.GetCurrentWeekStartUtc();
        var teamEmployeeIds = team.Select(employee => employee.Id).ToList();

        var timesheets = await _timesheets.GetTeamTimesheetsByWeekAsync(
            teamEmployeeIds,
            resolvedWeekStart,
            cancellationToken);

        var timesheetByEmployee = timesheets.ToDictionary(timesheet => timesheet.EmployeeId);
        var rows = new List<TeamTimesheetRowDto>();

        foreach (var employee in team)
        {
            var weekAllocations = employee.Allocations
                .Where(allocation => ActiveDateHelper.IsAllocationActiveDuringWeek(allocation, resolvedWeekStart))
                .ToList();

            if (weekAllocations.Count == 0)
            {
                continue;
            }

            timesheetByEmployee.TryGetValue(employee.Id, out var timesheet);
            var hasSubmittedTimesheet = timesheet is not null;

            foreach (var allocation in weekAllocations)
            {
                var entry = timesheet?.Entries.FirstOrDefault(e => e.ProjectId == allocation.ProjectId);
                var hours = entry?.Hours ?? 0;
                var status = hasSubmittedTimesheet
                    ? TimesheetStatus.Submitted.ToString()
                    : TimesheetStatus.Missed.ToString();

                rows.Add(new TeamTimesheetRowDto(
                    employee.User.FullName,
                    allocation.Project.Name,
                    hours,
                    status));
            }
        }

        return new TeamTimesheetsResponse(resolvedWeekStart, rows);
    }

    public async Task<ManagerEmployeeTimesheetDetailResponse> GetEmployeeTimesheetDetailAsync(
        int managerUserId,
        int employeeId,
        DateOnly? weekStart,
        CancellationToken cancellationToken = default)
    {
        var managerContext = await _context.ResolveAsync(managerUserId, cancellationToken);
        var employee = EntityGuard.EnsureFound(
            await _employees.GetTeamMemberAsync(managerContext.ManagerEmployeeId, employeeId, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        ManagerScopeGuard.EnsureEmployeeOnTeam(employee, managerContext.ManagerEmployeeId, managerUserId: managerUserId);

        var resolvedWeekStart = weekStart ?? ActiveDateHelper.GetCurrentWeekStartUtc();
        var timesheets = await _timesheets.GetTeamTimesheetsByWeekAsync(
            [employee.Id],
            resolvedWeekStart,
            cancellationToken);

        var timesheet = timesheets.FirstOrDefault();
        var hasSubmittedTimesheet = timesheet is not null;
        var weekAllocations = employee.Allocations
            .Where(allocation => ActiveDateHelper.IsAllocationActiveDuringWeek(allocation, resolvedWeekStart))
            .ToList();

        var entries = weekAllocations
            .Select(allocation =>
            {
                var entry = timesheet?.Entries.FirstOrDefault(e => e.ProjectId == allocation.ProjectId);
                return new ManagerTimesheetEntryDetailDto(
                    allocation.Project.Name,
                    entry?.Hours ?? 0,
                    ParseActivityTags(entry?.ActivityTags));
            })
            .ToList();

        var status = hasSubmittedTimesheet
            ? TimesheetStatus.Submitted.ToString()
            : TimesheetStatus.Missed.ToString();

        return new ManagerEmployeeTimesheetDetailResponse(
            employee.Id,
            employee.User.FullName,
            resolvedWeekStart,
            status,
            entries);
    }

    private static IReadOnlyList<string> ParseActivityTags(string? activityTags)
    {
        if (string.IsNullOrWhiteSpace(activityTags))
        {
            return Array.Empty<string>();
        }

        return activityTags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
