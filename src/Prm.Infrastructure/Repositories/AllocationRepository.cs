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
}
