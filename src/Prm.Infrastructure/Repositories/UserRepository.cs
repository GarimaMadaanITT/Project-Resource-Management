using Microsoft.EntityFrameworkCore;
using Prm.Application.Interfaces;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Infrastructure.Persistence;

namespace Prm.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly PrmDbContext _context;

    public UserRepository(PrmDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Users.AsNoTracking().OrderBy(u => u.Id).ToListAsync(cancellationToken);

    public Task<bool> ExistsUsernameAsync(string username, int? excludeUserId = null, CancellationToken cancellationToken = default) =>
        _context.Users.AnyAsync(
            u => u.Username == username && (excludeUserId == null || u.Id != excludeUserId),
            cancellationToken);

    public Task<bool> ExistsEmailAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default) =>
        _context.Users.AnyAsync(
            u => u.Email == email && (excludeUserId == null || u.Id != excludeUserId),
            cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<User> CreateWithEmployeeAsync(User user, Employee? employee, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);

        if (employee is not null)
        {
            employee.User = user;
            _context.Employees.Add(employee);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public Task<int> CountActiveAdminsAsync(int? excludeUserId = null, CancellationToken cancellationToken = default) =>
        _context.Users.CountAsync(
            u => u.Role == UserRole.Admin && u.IsActive && (excludeUserId == null || u.Id != excludeUserId),
            cancellationToken);

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
