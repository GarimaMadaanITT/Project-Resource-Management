namespace Prm.Application.Common;

public static class AuditConstants
{
    public static class EntityNames
    {
        public const string User = "User";
        public const string Employee = "Employee";
        public const string ResourceProfile = "ResourceProfile";
        public const string EmployeeSkill = "EmployeeSkill";
        public const string UserSkill = "UserSkill";
        public const string Project = "Project";
        public const string Milestone = "Milestone";
        public const string Allocation = "Allocation";
        public const string Timesheet = "Timesheet";
    }

    public static class Actions
    {
        public const string Created = "Created";
        public const string Updated = "Updated";
        public const string Removed = "Removed";
        public const string Deactivated = "Deactivated";
        public const string Reactivated = "Reactivated";
        public const string PasswordReset = "PasswordReset";
        public const string ManagerAssigned = "ManagerAssigned";
        public const string StatusChanged = "StatusChanged";
        public const string HealthChanged = "HealthChanged";
        public const string Ended = "Ended";
        public const string Submitted = "Submitted";
        public const string MissedTimesheetDetected = "MissedTimesheetDetected";
    }

    public static class Sources
    {
        public const string User = "User";
        public const string Scheduler = "Scheduler";
        public const string System = "System";
    }
}
