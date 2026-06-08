using Microsoft.EntityFrameworkCore;
using Prm.Application.Common;
using Prm.Application.Interfaces;
using Prm.Infrastructure.Persistence;

namespace Prm.Infrastructure.Services;

public class DatabaseHealthService : IDatabaseHealthService
{
    private const string DatabaseProviderName = "Neon PostgreSQL";

    private readonly PrmDbContext _context;

    public DatabaseHealthService(PrmDbContext context)
    {
        _context = context;
    }

    public async Task<DatabaseHealthStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
        if (!canConnect)
        {
            return new DatabaseHealthStatus(
                Connected: false,
                Provider: DatabaseProviderName,
                UserCount: 0,
                EmployeeCount: 0,
                ProjectCount: 0,
                BootstrapAdminExists: false,
                ErrorMessage: $"Cannot connect to {DatabaseProviderName}.");
        }

        var userCount = await _context.Users.CountAsync(cancellationToken);
        var employeeCount = await _context.Employees.CountAsync(cancellationToken);
        var projectCount = await _context.Projects.CountAsync(cancellationToken);
        var bootstrapAdminExists = await _context.Users.AnyAsync(
            user => user.Username == AuthConstants.BootstrapAdminUsername,
            cancellationToken);

        return new DatabaseHealthStatus(
            Connected: true,
            Provider: DatabaseProviderName,
            UserCount: userCount,
            EmployeeCount: employeeCount,
            ProjectCount: projectCount,
            BootstrapAdminExists: bootstrapAdminExists);
    }
}
