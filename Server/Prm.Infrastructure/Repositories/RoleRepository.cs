using Microsoft.EntityFrameworkCore;
using Prm.Application.Interfaces;
using Prm.Domain.Entities;
using Prm.Infrastructure.Persistence;

namespace Prm.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private static readonly string[] DefaultRoles = ["Admin", "Manager", "Employee"];

    private readonly PrmDbContext _context;

    public RoleRepository(PrmDbContext context)
    {
        _context = context;
    }

    public Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default) =>
        _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName, cancellationToken);

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in DefaultRoles)
        {
            if (!await _context.Roles.AnyAsync(r => r.RoleName == roleName, cancellationToken))
            {
                _context.Roles.Add(new Role { RoleName = roleName });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
