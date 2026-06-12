using Microsoft.EntityFrameworkCore;
using Prm.Application.Interfaces;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Infrastructure.Persistence;

namespace Prm.Infrastructure.Repositories;

public class ResourceProfileRepository : IResourceProfileRepository
{
    private readonly PrmDbContext _context;

    public ResourceProfileRepository(PrmDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ResourceProfile>> GetAllAsync(
        string? department,
        ResourceStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ResourceProfiles
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Allocations)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(department))
        {
            if (Enum.TryParse<Department>(department, true, out var dept))
            {
                query = query.Where(r => r.User.Department == dept);
            }
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.ResourceStatus == status.Value);
        }

        return await query.OrderBy(r => r.Id).ToListAsync(cancellationToken);
    }

    public Task<ResourceProfile?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.ResourceProfiles
            .Include(r => r.User).ThenInclude(u => u.Skills).ThenInclude(s => s.Skill)
            .Include(r => r.Allocations).ThenInclude(a => a.Project)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<ResourceProfile?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.ResourceProfiles
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken);

    public async Task AddAsync(ResourceProfile resourceProfile, CancellationToken cancellationToken = default)
    {
        _context.ResourceProfiles.Add(resourceProfile);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ResourceProfile resourceProfile, CancellationToken cancellationToken = default)
    {
        resourceProfile.UpdatedAt = DateTime.UtcNow;
        _context.ResourceProfiles.Update(resourceProfile);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetActiveAllocationsAsync(int resourceProfileId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _context.Allocations
            .Include(a => a.Project)
            .Where(a => a.ResourceProfileId == resourceProfileId && a.FromDate <= today && a.ToDate >= today)
            .ToListAsync(cancellationToken);
    }

    public async Task EndActiveAllocationsAsync(int resourceProfileId, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allocations = await _context.Allocations
            .Where(a => a.ResourceProfileId == resourceProfileId && a.FromDate <= today && a.ToDate >= today)
            .ToListAsync(cancellationToken);

        foreach (var allocation in allocations)
        {
            allocation.ToDate = endDate;
            allocation.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasActiveTeamMembersAsync(int managerUserId, CancellationToken cancellationToken = default) =>
        _context.ResourceProfiles.AnyAsync(
            r => r.ManagerUserId == managerUserId && r.User.IsActive,
            cancellationToken);

    public async Task<IReadOnlyList<ResourceProfile>> GetTeamByManagerUserIdAsync(
        int managerUserId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ResourceProfiles
            .AsNoTracking()
            .Include(r => r.User).ThenInclude(u => u.Skills).ThenInclude(s => s.Skill)
            .Include(r => r.Allocations).ThenInclude(a => a.Project)
            .Where(r => r.ManagerUserId == managerUserId);

        if (activeOnly)
        {
            query = query.Where(r => r.User.IsActive);
        }

        return await query.OrderBy(r => r.Id).ToListAsync(cancellationToken);
    }

    public Task<ResourceProfile?> GetTeamMemberAsync(
        int managerUserId,
        int resourceProfileId,
        CancellationToken cancellationToken = default) =>
        _context.ResourceProfiles
            .Include(r => r.User).ThenInclude(u => u.Skills).ThenInclude(s => s.Skill)
            .Include(r => r.Allocations).ThenInclude(a => a.Project)
            .FirstOrDefaultAsync(
                r => r.Id == resourceProfileId && r.ManagerUserId == managerUserId,
                cancellationToken);

    public async Task<IReadOnlyList<ResourceProfile>> GetAllActiveWithAllocationsAsync(CancellationToken cancellationToken = default) =>
        await _context.ResourceProfiles
            .Include(r => r.Allocations)
            .Include(r => r.User)
            .Where(r => r.User.IsActive)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ResourceProfile>> GetOrgWideCandidatesAsync(CancellationToken cancellationToken = default) =>
        await _context.ResourceProfiles
            .AsNoTracking()
            .Include(r => r.User).ThenInclude(u => u.Skills).ThenInclude(s => s.Skill)
            .Include(r => r.Allocations).ThenInclude(a => a.Project)
            .Where(r => r.User.IsActive)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);
}
