using Prm.Application.Common;
using Microsoft.EntityFrameworkCore;
using Prm.Application.Interfaces;
using Prm.Domain.Entities;
using Prm.Infrastructure.Persistence;

namespace Prm.Infrastructure.Repositories;

public class SystemSettingsRepository : ISystemSettingsRepository
{
    private readonly PrmDbContext _context;

    public SystemSettingsRepository(PrmDbContext context)
    {
        _context = context;
    }

    public async Task<SystemSetting> GetAsync(CancellationToken cancellationToken = default) =>
        await _context.SystemSettings.FirstOrDefaultAsync(cancellationToken)
        ?? throw new KeyNotFoundException(ErrorMessages.SystemSettingsNotFound);

    public async Task UpdateAsync(SystemSetting settings, CancellationToken cancellationToken = default)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        _context.SystemSettings.Update(settings);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
