using Prm.Application.Common;
using Prm.Application.DTOs.Auth;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Exceptions;

namespace Prm.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var username = StringGuard.RequireNonEmpty(request.Username, "Username");
        var password = StringGuard.RequireNonEmpty(request.Password, "Password");

        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (user is null || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Your account is inactive. Contact an administrator.");
        }

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        return new LoginResponse(
            token,
            expiresAt,
            MapUser(user),
            user.ForcePasswordChange);
    }

    public async Task<ChangePasswordResponse> ChangePasswordAsync(
        int userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new DomainException("New password and confirmation do not match.");
        }

        var newPassword = StringGuard.RequireNonEmpty(request.NewPassword, "New password");
        StringGuard.RequireNonEmpty(request.ConfirmPassword, "Confirm password");

        if (!PasswordValidator.IsValid(newPassword, out var validationError))
        {
            throw new DomainException(validationError);
        }

        var user = EntityGuard.EnsureFound(
            await _userRepository.GetByIdAsync(userId, cancellationToken),
            ErrorMessages.UserNotFound);

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Your account is inactive.");
        }

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.ForcePasswordChange = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        return new ChangePasswordResponse(
            "Password updated successfully.",
            false,
            token,
            expiresAt);
    }

    public async Task<AuthenticatedUserDto> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = EntityGuard.EnsureFound(
            await _userRepository.GetByIdAsync(userId, cancellationToken),
            ErrorMessages.UserNotFound);

        return MapUser(user);
    }

    private static AuthenticatedUserDto MapUser(Domain.Entities.User user) =>
        new(
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            AuthConstants.RoleName(user.Role));
}
