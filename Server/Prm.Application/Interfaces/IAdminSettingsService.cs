using Prm.Application.DTOs.Admin;

namespace Prm.Application.Interfaces;

public interface IAdminSettingsService
{
    Task<SystemSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<SystemSettingsDto> UpdateAsync(UpdateSystemSettingsRequest request, CancellationToken cancellationToken = default);
}
