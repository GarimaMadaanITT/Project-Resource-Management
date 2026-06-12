using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Application.Common;

public static class AuthConstants
{
    public const string TemporaryPasswordClaim = "is_temporary_password";
    public const string ForcePasswordChangeClaim = TemporaryPasswordClaim;
    public const string BootstrapAdminUsername = "admin";

    public static class PolicyNames
    {
        public const string AdminOnly = "AdminOnly";
        public const string ManagerOnly = "ManagerOnly";
        public const string EmployeeOnly = "EmployeeOnly";
    }

    public static string RoleName(UserRole role) => role switch
    {
        UserRole.Admin => "Admin",
        UserRole.Manager => "Manager",
        UserRole.Employee => "Employee",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    public static UserRole ParseRole(string role) =>
        role.Trim().ToUpperInvariant() switch
        {
            "ADMIN" => UserRole.Admin,
            "MANAGER" => UserRole.Manager,
            "EMPLOYEE" => UserRole.Employee,
            _ => throw new DomainException(ErrorMessages.InvalidRole)
        };
}
