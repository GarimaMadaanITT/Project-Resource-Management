using Prm.Application.DTOs.Auth;

namespace Prm.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ChangePasswordResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticatedUserDto> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
}
