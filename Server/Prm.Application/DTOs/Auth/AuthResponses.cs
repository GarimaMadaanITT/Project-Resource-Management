namespace Prm.Application.DTOs.Auth;

public record AuthenticatedUserDto(
    int Id,
    string Username,
    string FullName,
    string Email,
    string Role);

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    AuthenticatedUserDto User,
    bool IsTemporaryPassword);

public record ChangePasswordResponse(
    string Message,
    bool IsTemporaryPassword,
    string? Token = null,
    DateTime? ExpiresAt = null);
