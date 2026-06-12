using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Common;

public static class AuditSnapshotBuilder
{
    public static object UserSnapshot(User user) =>
        new
        {
            user.Id,
            user.Username,
            Role = user.Role.ToString(),
            user.IsActive,
            user.ForcePasswordChange
        };

    public static object PasswordResetSnapshot(bool forcePasswordChange) =>
        new { forcePasswordChange };

    public static object EmployeeSnapshot(Employee employee) =>
        new
        {
            employee.Id,
            employee.Department,
            Status = employee.Status.ToString(),
            employee.IsActive,
            employee.ManagerId
        };

    public static object EmployeeSkillSnapshot(EmployeeSkill employeeSkill) =>
        new
        {
            employeeSkill.EmployeeId,
            employeeSkill.SkillId,
            Proficiency = employeeSkill.Proficiency.ToString()
        };

    public static object ProjectSnapshot(Project project) =>
        new
        {
            project.Id,
            project.Name,
            Status = project.Status.ToString(),
            HealthStatus = project.HealthStatus.ToString(),
            project.ManagerUserId,
            project.StartDate,
            project.EndDate,
            project.TotalStoryPoints
        };

    public static object MilestoneSnapshot(Milestone milestone) =>
        new
        {
            milestone.Id,
            milestone.ProjectId,
            milestone.Title,
            milestone.DueDate,
            milestone.StoryPoints,
            Status = milestone.Status.ToString()
        };

    public static object AllocationSnapshot(Allocation allocation) =>
        new
        {
            allocation.Id,
            allocation.EmployeeId,
            allocation.ProjectId,
            allocation.UtilisationPercent,
            allocation.FromDate,
            allocation.ToDate
        };

    public static object TimesheetSnapshot(Timesheet timesheet) =>
        new
        {
            timesheet.Id,
            timesheet.EmployeeId,
            timesheet.WeekStart,
            Status = timesheet.Status.ToString(),
            timesheet.TotalHours
        };

    public static object MissedTimesheetSnapshot(DateOnly weekStart, int employeeId) =>
        new { employeeId, weekStart };
}
