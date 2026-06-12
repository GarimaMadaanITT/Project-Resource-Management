using Microsoft.EntityFrameworkCore;
using Prm.Application.Interfaces;
using Prm.Domain.Entities;
using Prm.Infrastructure.Persistence;

namespace Prm.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly PrmDbContext _context;

    public ProjectRepository(PrmDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Projects
            .AsNoTracking()
            .Include(p => p.Manager)
            .Include(p => p.Milestones)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Project>> GetByManagerUserIdAsync(
        int managerUserId,
        CancellationToken cancellationToken = default) =>
        await _context.Projects
            .AsNoTracking()
            .Include(p => p.Manager)
            .Include(p => p.Milestones)
            .Where(p => p.ManagerUserId == managerUserId)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

    public Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Projects
            .Include(p => p.Manager)
            .Include(p => p.Milestones)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Project?> GetByIdWithAllocationsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Projects
            .Include(p => p.Manager)
            .Include(p => p.Milestones)
            .Include(p => p.Allocations).ThenInclude(a => a.ResourceProfile).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        project.UpdatedAt = DateTime.UtcNow;
        _context.Projects.Update(project);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Milestone?> GetMilestoneAsync(int projectId, int milestoneId, CancellationToken cancellationToken = default) =>
        _context.Milestones.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.Id == milestoneId, cancellationToken);

    public async Task AddMilestoneAsync(Milestone milestone, CancellationToken cancellationToken = default)
    {
        _context.Milestones.Add(milestone);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateMilestoneAsync(Milestone milestone, CancellationToken cancellationToken = default)
    {
        milestone.UpdatedAt = DateTime.UtcNow;
        _context.Milestones.Update(milestone);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasActiveProjectsForManagerAsync(int managerUserId, CancellationToken cancellationToken = default) =>
        _context.Projects.AnyAsync(
            p => p.ManagerUserId == managerUserId && p.Status == Domain.Enums.ProjectStatus.Active,
            cancellationToken);

    public async Task<int> GetMilestoneStoryPointsSumAsync(
        int projectId,
        int? excludeMilestoneId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Milestones.Where(m => m.ProjectId == projectId);

        if (excludeMilestoneId.HasValue)
        {
            query = query.Where(m => m.Id != excludeMilestoneId.Value);
        }

        return await query.SumAsync(m => m.StoryPoints, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetAllActiveWithDetailsAsync(CancellationToken cancellationToken = default) =>
        await _context.Projects
            .Include(p => p.Milestones)
            .Include(p => p.Allocations).ThenInclude(a => a.ResourceProfile).ThenInclude(r => r.User)
            .Where(p => p.Status == Domain.Enums.ProjectStatus.Active)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);
}
