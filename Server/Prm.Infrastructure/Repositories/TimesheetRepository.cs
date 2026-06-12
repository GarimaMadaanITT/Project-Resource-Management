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
        IReadOnlyList<int> teamResourceProfileIds,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        if (teamResourceProfileIds.Count == 0)
        {
            return Array.Empty<Timesheet>();
        }

        return await _context.Timesheets
            .AsNoTracking()
            .Include(t => t.ResourceProfile).ThenInclude(r => r.User)
            .Include(t => t.Entries).ThenInclude(e => e.Project)
            .Where(t => teamResourceProfileIds.Contains(t.ResourceProfileId) && t.WeekStart == weekStart)
            .OrderBy(t => t.ResourceProfileId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetRecentActivityTagsAsync(
        int resourceProfileId,
        int weeks = 4,
        CancellationToken cancellationToken = default)
    {
        var recentTimesheetIds = await _context.Timesheets
            .AsNoTracking()
            .Where(timesheet => timesheet.ResourceProfileId == resourceProfileId)
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

    public async Task<IReadOnlyList<TimesheetEntry>> GetEntriesForResourceProfilesAndWeekAsync(
        IReadOnlyList<int> resourceProfileIds,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        if (resourceProfileIds.Count == 0)
        {
            return Array.Empty<TimesheetEntry>();
        }

        return await _context.TimesheetEntries
            .AsNoTracking()
            .Include(entry => entry.Timesheet)
            .Include(entry => entry.Project)
            .Where(entry =>
                resourceProfileIds.Contains(entry.Timesheet.ResourceProfileId)
                && entry.Timesheet.WeekStart == weekStart)
            .ToListAsync(cancellationToken);
    }

    public Task<Timesheet?> GetByResourceProfileAndWeekAsync(
        int resourceProfileId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default) =>
        _context.Timesheets
            .AsNoTracking()
            .Include(t => t.Entries).ThenInclude(e => e.Project)
            .FirstOrDefaultAsync(
                t => t.ResourceProfileId == resourceProfileId && t.WeekStart == weekStart,
                cancellationToken);

    public Task<bool> ExistsForResourceProfileWeekAsync(
        int resourceProfileId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default) =>
        _context.Timesheets.AnyAsync(
            t => t.ResourceProfileId == resourceProfileId && t.WeekStart == weekStart,
            cancellationToken);

    public async Task<IReadOnlyList<Timesheet>> GetByResourceProfileIdAsync(
        int resourceProfileId,
        CancellationToken cancellationToken = default) =>
        await _context.Timesheets
            .AsNoTracking()
            .Include(t => t.Entries).ThenInclude(e => e.Project)
            .Where(t => t.ResourceProfileId == resourceProfileId)
            .OrderByDescending(t => t.WeekStart)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Timesheet timesheet, CancellationToken cancellationToken = default)
    {
        _context.Timesheets.Add(timesheet);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
