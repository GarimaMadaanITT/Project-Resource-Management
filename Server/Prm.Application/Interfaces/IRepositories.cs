using Prm.Application.DTOs.Admin;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsUsernameAsync(string username, int? excludeUserId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsEmailAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User> CreateWithResourceProfileAsync(
        User user,
        ResourceProfile? resourceProfile,
        UserRoleAssignment roleAssignment,
        CancellationToken cancellationToken = default);
    Task<int> CountActiveAdminsAsync(int? excludeUserId = null, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task EnsureSeededAsync(CancellationToken cancellationToken = default);
}

public interface IResourceProfileRepository
{
    Task<IReadOnlyList<ResourceProfile>> GetAllAsync(string? department, ResourceStatus? status, CancellationToken cancellationToken = default);
    Task<ResourceProfile?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ResourceProfile?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task AddAsync(ResourceProfile resourceProfile, CancellationToken cancellationToken = default);
    Task UpdateAsync(ResourceProfile resourceProfile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetActiveAllocationsAsync(int resourceProfileId, CancellationToken cancellationToken = default);
    Task EndActiveAllocationsAsync(int resourceProfileId, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<bool> HasActiveTeamMembersAsync(int managerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResourceProfile>> GetTeamByManagerUserIdAsync(
        int managerUserId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
    Task<ResourceProfile?> GetTeamMemberAsync(
        int managerUserId,
        int resourceProfileId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResourceProfile>> GetAllActiveWithAllocationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResourceProfile>> GetOrgWideCandidatesAsync(CancellationToken cancellationToken = default);
}

public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByManagerUserIdAsync(int managerUserId, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdWithAllocationsAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);
    Task<Milestone?> GetMilestoneAsync(int projectId, int milestoneId, CancellationToken cancellationToken = default);
    Task AddMilestoneAsync(Milestone milestone, CancellationToken cancellationToken = default);
    Task UpdateMilestoneAsync(Milestone milestone, CancellationToken cancellationToken = default);
    Task<bool> HasActiveProjectsForManagerAsync(int managerUserId, CancellationToken cancellationToken = default);
    Task<int> GetMilestoneStoryPointsSumAsync(int projectId, int? excludeMilestoneId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetAllActiveWithDetailsAsync(CancellationToken cancellationToken = default);
}

public interface IAllocationRepository
{
    Task<IReadOnlyList<Allocation>> GetAllAsync(int? resourceProfileId, int? projectId, CancellationToken cancellationToken = default);
    Task<Allocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetByResourceProfileIdAsync(int resourceProfileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetActiveByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task AddAsync(Allocation allocation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Allocation allocation, CancellationToken cancellationToken = default);
}

public interface ITimesheetRepository
{
    Task<IReadOnlyList<Timesheet>> GetTeamTimesheetsByWeekAsync(
        IReadOnlyList<int> teamResourceProfileIds,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRecentActivityTagsAsync(
        int resourceProfileId,
        int weeks = 4,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TimesheetEntry>> GetEntriesForResourceProfilesAndWeekAsync(
        IReadOnlyList<int> resourceProfileIds,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
    Task<Timesheet?> GetByResourceProfileAndWeekAsync(
        int resourceProfileId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsForResourceProfileWeekAsync(
        int resourceProfileId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Timesheet>> GetByResourceProfileIdAsync(
        int resourceProfileId,
        CancellationToken cancellationToken = default);
    Task AddAsync(Timesheet timesheet, CancellationToken cancellationToken = default);
}

public interface ISystemSettingsRepository
{
    Task<SystemSetting> GetAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(SystemSetting settings, CancellationToken cancellationToken = default);
}

public interface ISkillRepository
{
    Task<Skill?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Skill skill, CancellationToken cancellationToken = default);
    Task<UserSkill?> GetUserSkillAsync(int userId, int skillId, CancellationToken cancellationToken = default);
    Task AddUserSkillAsync(UserSkill userSkill, CancellationToken cancellationToken = default);
    Task RemoveUserSkillAsync(UserSkill userSkill, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> QueryAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);
}
