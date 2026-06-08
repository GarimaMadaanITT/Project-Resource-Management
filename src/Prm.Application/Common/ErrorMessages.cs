namespace Prm.Application.Common;

public static class ErrorMessages
{
    public const string EmployeeNotFound = "Employee not found.";
    public const string UserNotFound = "User not found.";
    public const string ProjectNotFound = "Project not found.";
    public const string MilestoneNotFound = "Milestone not found.";
    public const string EmployeeSkillNotFound = "Employee skill not found.";
    public const string LinkedUserNotFound = "Linked user not found.";
    public const string ManagerUserNotFound = "Manager user not found.";

    public const string EmployeeAlreadyInactive = "Employee is already inactive.";
    public const string UserAlreadyInactive = "User is already inactive.";

    public const string UsernameAlreadyExists = "Username already exists.";
    public const string EmailAlreadyExists = "Email already exists.";
    public const string EmployeeSkillDuplicate = "Employee already has this skill.";

    public const string InvalidRole = "Role must be Admin, Manager, or Employee.";
    public const string InvalidToken = "Invalid token.";
    public const string SystemSettingsNotFound = "System settings not found.";
}
