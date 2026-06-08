using Prm.Application.DTOs.Admin;

namespace Prm.Application.Interfaces;

public interface IAdminEmployeeService
{
    Task<EmployeeListResponse> GetAllAsync(string? department, string? status, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<DeactivateEmployeeResponse> DeactivateAsync(int id, int actingUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeSkillDto>> GetSkillsAsync(int id, CancellationToken cancellationToken = default);
    Task<EmployeeSkillDto> AddSkillAsync(int id, AddEmployeeSkillRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeSkillDto> UpdateSkillAsync(int id, int skillId, UpdateEmployeeSkillRequest request, CancellationToken cancellationToken = default);
    Task RemoveSkillAsync(int id, int skillId, CancellationToken cancellationToken = default);
    Task AssignManagerAsync(int id, AssignManagerRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminUserService
{
    Task<UserListItemDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task DeactivateAsync(int id, int actingUserId, CancellationToken cancellationToken = default);
    Task ReactivateAsync(int id, CancellationToken cancellationToken = default);
}

public interface IAdminProjectService
{
    Task<IReadOnlyList<ProjectListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProjectListItemDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<ProjectListItemDto> UpdateAsync(int id, UpdateProjectRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MilestoneDto>> GetMilestonesAsync(int projectId, CancellationToken cancellationToken = default);
    Task<MilestoneDto> AddMilestoneAsync(int projectId, AddMilestoneRequest request, CancellationToken cancellationToken = default);
    Task<MilestoneDto> UpdateMilestoneStatusAsync(int projectId, int milestoneId, UpdateMilestoneStatusRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminAllocationService
{
    Task<IReadOnlyList<AllocationListItemDto>> GetAllAsync(int? employeeId, int? projectId, CancellationToken cancellationToken = default);
}

public interface IAdminSettingsService
{
    Task<SystemSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<SystemSettingsDto> UpdateAsync(UpdateSystemSettingsRequest request, CancellationToken cancellationToken = default);
}
