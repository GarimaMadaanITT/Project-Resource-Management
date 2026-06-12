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
    Task<User> CreateWithEmployeeAsync(User user, Employee? employee, CancellationToken cancellationToken = default);
    Task<int> CountActiveAdminsAsync(int? excludeUserId = null, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}

public interface IEmployeeRepository
{
    Task<IReadOnlyList<Employee>> GetAllAsync(string? department, EmployeeStatus? status, CancellationToken cancellationToken = default);
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Employee?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetActiveAllocationsAsync(int employeeId, CancellationToken cancellationToken = default);
    Task EndActiveAllocationsAsync(int employeeId, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<bool> HasActiveTeamMembersAsync(int managerEmployeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetTeamByManagerEmployeeIdAsync(
        int managerEmployeeId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
    Task<Employee?> GetTeamMemberAsync(
        int managerEmployeeId,
        int employeeId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetAllActiveWithAllocationsAsync(CancellationToken cancellationToken = default);
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
    Task<IReadOnlyList<Allocation>> GetAllAsync(int? employeeId, int? projectId, CancellationToken cancellationToken = default);
    Task<Allocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetActiveByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task AddAsync(Allocation allocation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Allocation allocation, CancellationToken cancellationToken = default);
}

public interface ITimesheetRepository
{
    Task<IReadOnlyList<Timesheet>> GetTeamTimesheetsByWeekAsync(
        IReadOnlyList<int> teamEmployeeIds,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRecentActivityTagsAsync(
        int employeeId,
        int weeks = 4,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TimesheetEntry>> GetEntriesForEmployeesAndWeekAsync(
        IReadOnlyList<int> employeeIds,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
    Task<Timesheet?> GetByEmployeeAndWeekAsync(
        int employeeId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsForEmployeeWeekAsync(
        int employeeId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Timesheet>> GetByEmployeeIdAsync(
        int employeeId,
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
    Task<EmployeeSkill?> GetEmployeeSkillAsync(int employeeId, int skillId, CancellationToken cancellationToken = default);
    Task AddEmployeeSkillAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken = default);
    Task RemoveEmployeeSkillAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> QueryAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);
}
