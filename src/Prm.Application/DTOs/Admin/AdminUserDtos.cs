namespace Prm.Application.DTOs.Admin;

public record CreateUserRequest(
    string FullName,
    string Email,
    string Username,
    string TemporaryPassword,
    string Role,
    string? Department);

public record UserListItemDto(
    int Id,
    string Username,
    string FullName,
    string Role,
    bool IsActive);

public record ResetPasswordRequest(string NewTemporaryPassword);
