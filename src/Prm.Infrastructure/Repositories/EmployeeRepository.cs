using Microsoft.EntityFrameworkCore;
using Prm.Application.Interfaces;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Infrastructure.Persistence;

namespace Prm.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly PrmDbContext _context;

    public EmployeeRepository(PrmDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Employee>> GetAllAsync(
        string? department,
        EmployeeStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Allocations)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(e => e.Department == department);
        }

        if (status.HasValue)
        {
            query = query.Where(employee => employee.Status == status.Value);
        }

        return await query.OrderBy(employee => employee.Id).ToListAsync(cancellationToken);
    }

    public Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Employees
            .Include(e => e.User)
            .Include(e => e.Skills).ThenInclude(s => s.Skill)
            .Include(e => e.Allocations)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Employee?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        employee.UpdatedAt = DateTime.UtcNow;
        _context.Employees.Update(employee);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetActiveAllocationsAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _context.Allocations
            .Where(a => a.EmployeeId == employeeId && a.FromDate <= today && a.ToDate >= today)
            .ToListAsync(cancellationToken);
    }

    public async Task EndActiveAllocationsAsync(int employeeId, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allocations = await _context.Allocations
            .Where(a => a.EmployeeId == employeeId && a.FromDate <= today && a.ToDate >= today)
            .ToListAsync(cancellationToken);

        foreach (var allocation in allocations)
        {
            allocation.ToDate = endDate;
            allocation.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasActiveTeamMembersAsync(int managerEmployeeId, CancellationToken cancellationToken = default) =>
        _context.Employees.AnyAsync(
            e => e.ManagerId == managerEmployeeId && e.IsActive,
            cancellationToken);
}
