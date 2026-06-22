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
            Role = UserRoleHelper.GetPrimaryRoleName(user),
            user.IsActive,
            user.IsTemporaryPassword
        };

    public static object PasswordResetSnapshot(bool isTemporaryPassword) =>
        new { isTemporaryPassword };

    public static object ResourceProfileSnapshot(ResourceProfile resourceProfile) =>
        new
        {
            resourceProfile.Id,
            Department = resourceProfile.User.Department?.ToString(),
            Status = resourceProfile.ResourceStatus.ToString(),
            resourceProfile.User.IsActive,
            resourceProfile.ManagerUserId
        };

    public static object UserSkillSnapshot(UserSkill userSkill) =>
        new
        {
            userSkill.UserId,
            userSkill.SkillId,
            Proficiency = userSkill.Proficiency.ToString()
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
            allocation.ResourceProfileId,
            allocation.ProjectId,
            allocation.UtilisationPercent,
            allocation.FromDate,
            allocation.ToDate
        };

    public static object TimesheetSnapshot(Timesheet timesheet) =>
        new
        {
            timesheet.Id,
            timesheet.ResourceProfileId,
            timesheet.WeekStart,
            Status = timesheet.Status.ToString(),
            timesheet.TotalHours
        };

    public static object MissedTimesheetSnapshot(DateOnly weekStart, int resourceProfileId) =>
        new { resourceProfileId, weekStart };

    public static object TimesheetFreezeSnapshot(int resourceProfileId, DateOnly weekStart) =>
        new { resourceProfileId, weekStart, frozen = true };
}
