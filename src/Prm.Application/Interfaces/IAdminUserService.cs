using Prm.Application.DTOs.Admin;

namespace Prm.Application.Interfaces;

public interface IAdminUserService
{
    Task<UserListItemDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task DeactivateAsync(int id, int actingUserId, CancellationToken cancellationToken = default);
    Task ReactivateAsync(int id, CancellationToken cancellationToken = default);
}
