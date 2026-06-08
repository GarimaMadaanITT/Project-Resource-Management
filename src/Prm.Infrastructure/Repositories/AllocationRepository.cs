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
        int? employeeId,
        int? projectId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Allocations
            .AsNoTracking()
            .Include(a => a.Employee).ThenInclude(e => e.User)
            .Include(a => a.Project)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(a => a.ProjectId == projectId.Value);
        }

        return await query.OrderBy(a => a.EmployeeId).ToListAsync(cancellationToken);
    }

    public Task<Allocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Allocations
            .Include(a => a.Employee).ThenInclude(e => e.User)
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Allocation>> GetByEmployeeIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default) =>
        await _context.Allocations
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .OrderBy(a => a.FromDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Allocation>> GetActiveByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _context.Allocations
            .AsNoTracking()
            .Include(a => a.Employee).ThenInclude(e => e.User)
            .Where(a => a.ProjectId == projectId && a.FromDate <= today && a.ToDate >= today)
            .OrderBy(a => a.EmployeeId)
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
