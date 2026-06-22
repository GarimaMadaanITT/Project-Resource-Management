using Prm.Application.DTOs.Admin;
using Prm.Domain.Entities;

namespace Prm.Application.Interfaces;

public interface IAccountDeactivationService
{
    Task<DeactivateEmployeeResponse> DeactivateEmployeeAsync(
        ResourceProfile resourceProfile,
        User user,
        int actingUserId,
        CancellationToken cancellationToken = default);

    Task DeactivateUserAsync(User user, int actingUserId, CancellationToken cancellationToken = default);
}
