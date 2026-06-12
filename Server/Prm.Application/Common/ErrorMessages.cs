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
    public const string UserRoleNotFound = "User role not found.";
    public const string SystemSettingsNotFound = "System settings not found.";
    public const string AllocationNotFound = "Allocation not found.";
    public const string ManagerEmployeeProfileNotFound = "Manager employee profile not found.";
    public const string EmployeeProfileNotFound = "Employee profile not found.";
    public const string EmployeeNotOnManagerTeam = "Employee is not on your team.";
    public const string ProjectNotOwnedByManager = "You do not manage this project.";
    public const string TimesheetAlreadySubmitted = "A timesheet for this week has already been submitted.";
    public const string TimesheetNotFound = "Timesheet not found for this week.";
}
