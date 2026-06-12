namespace Prm.Application.DTOs.Auth;

public record ChangePasswordRequest(string NewPassword, string ConfirmPassword);
