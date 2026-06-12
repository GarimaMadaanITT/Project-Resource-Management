using Microsoft.EntityFrameworkCore;
using Prm.Application.Interfaces;
using Prm.Domain.Entities;
using Prm.Infrastructure.Persistence;

namespace Prm.Infrastructure.Repositories;

public class AllocationRepository : IAllocationRepository
{
    private readonly PrmDbContext _context;

    public AllocationRepository(PrmDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Allocation>> GetAllAsync(
        int? resourceProfileId,
        int? projectId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Allocations
            .AsNoTracking()
            .Include(a => a.ResourceProfile).ThenInclude(r => r.User)
            .Include(a => a.Project)
            .AsQueryable();

        if (resourceProfileId.HasValue)
        {
            query = query.Where(a => a.ResourceProfileId == resourceProfileId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(a => a.ProjectId == projectId.Value);
        }

        return await query.OrderBy(a => a.ResourceProfileId).ToListAsync(cancellationToken);
    }

    public Task<Allocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Allocations
            .Include(a => a.ResourceProfile).ThenInclude(r => r.User)
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Allocation>> GetByResourceProfileIdAsync(
        int resourceProfileId,
        CancellationToken cancellationToken = default) =>
        await _context.Allocations
            .AsNoTracking()
            .Where(a => a.ResourceProfileId == resourceProfileId)
            .OrderBy(a => a.FromDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Allocation>> GetActiveByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _context.Allocations
            .AsNoTracking()
            .Include(a => a.ResourceProfile).ThenInclude(r => r.User)
            .Where(a => a.ProjectId == projectId && a.FromDate <= today && a.ToDate >= today)
            .OrderBy(a => a.ResourceProfileId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Allocation allocation, CancellationToken cancellationToken = default)
    {
        _context.Allocations.Add(allocation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Allocation allocation, CancellationToken cancellationToken = default)
    {
        allocation.UpdatedAt = DateTime.UtcNow;
        _context.Allocations.Update(allocation);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
