using Microsoft.EntityFrameworkCore;
using Prm.Application.Interfaces;
using Prm.Domain.Entities;
using Prm.Infrastructure.Persistence;

namespace Prm.Infrastructure.Repositories;

public class TimesheetRepository : ITimesheetRepository
{
    private readonly PrmDbContext _context;

    public TimesheetRepository(PrmDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Timesheet>> GetTeamTimesheetsByWeekAsync(
        IReadOnlyList<int> teamEmployeeIds,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        if (teamEmployeeIds.Count == 0)
        {
            return Array.Empty<Timesheet>();
        }

        return await _context.Timesheets
            .AsNoTracking()
            .Include(t => t.Employee).ThenInclude(e => e.User)
            .Include(t => t.Entries).ThenInclude(e => e.Project)
            .Where(t => teamEmployeeIds.Contains(t.EmployeeId) && t.WeekStart == weekStart)
            .OrderBy(t => t.EmployeeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetRecentActivityTagsAsync(
        int employeeId,
        int weeks = 4,
        CancellationToken cancellationToken = default)
    {
        var recentTimesheetIds = await _context.Timesheets
            .AsNoTracking()
            .Where(timesheet => timesheet.EmployeeId == employeeId)
            .OrderByDescending(timesheet => timesheet.WeekStart)
            .Take(weeks)
            .Select(timesheet => timesheet.Id)
            .ToListAsync(cancellationToken);

        if (recentTimesheetIds.Count == 0)
        {
            return Array.Empty<string>();
        }

        var tags = await _context.TimesheetEntries
            .AsNoTracking()
            .Where(entry => recentTimesheetIds.Contains(entry.TimesheetId))
            .Select(entry => entry.ActivityTags)
            .ToListAsync(cancellationToken);

        return tags
            .SelectMany(tagString => tagString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag)
            .ToList();
    }

    public async Task<IReadOnlyList<TimesheetEntry>> GetEntriesForEmployeesAndWeekAsync(
        IReadOnlyList<int> employeeIds,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        if (employeeIds.Count == 0)
        {
            return Array.Empty<TimesheetEntry>();
        }

        return await _context.TimesheetEntries
            .AsNoTracking()
            .Include(entry => entry.Timesheet)
            .Include(entry => entry.Project)
            .Where(entry =>
                employeeIds.Contains(entry.Timesheet.EmployeeId)
                && entry.Timesheet.WeekStart == weekStart)
            .ToListAsync(cancellationToken);
    }
}
