using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Services.Admin.Employees;
using Prm.Application.Validation;

namespace Prm.Application.Services.Admin;

public class AdminEmployeeService : IAdminEmployeeService
{
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly IUserRepository _users;
    private readonly IAccountDeactivationService _accountDeactivation;
    private readonly AdminEmployeeQueryService _queryService;
    private readonly AdminEmployeeCommandService _commandService;
    private readonly AdminEmployeeSkillService _skillService;

    public AdminEmployeeService(
        IResourceProfileRepository resourceProfiles,
        IUserRepository users,
        IAccountDeactivationService accountDeactivation,
        AdminEmployeeQueryService queryService,
        AdminEmployeeCommandService commandService,
        AdminEmployeeSkillService skillService)
    {
        _resourceProfiles = resourceProfiles;
        _users = users;
        _accountDeactivation = accountDeactivation;
        _queryService = queryService;
        _commandService = commandService;
        _skillService = skillService;
    }

    public Task<EmployeeListResponse> GetAllAsync(string? department, string? status, CancellationToken cancellationToken = default) =>
        _queryService.GetAllAsync(department, status, cancellationToken);

    public Task UpdateAsync(int id, UpdateEmployeeRequest request, int actingUserId, CancellationToken cancellationToken = default) =>
        _commandService.UpdateAsync(id, request, actingUserId, cancellationToken);

    public async Task<DeactivateEmployeeResponse> DeactivateAsync(int id, int actingUserId, CancellationToken cancellationToken = default)
    {
        var resourceProfile = EntityGuard.EnsureFound(
            await _resourceProfiles.GetByIdAsync(id, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        var user = EntityGuard.EnsureFound(
            await _users.GetByIdAsync(resourceProfile.UserId, cancellationToken),
            ErrorMessages.LinkedUserNotFound);

        return await _accountDeactivation.DeactivateEmployeeAsync(resourceProfile, user, actingUserId, cancellationToken);
    }

    public Task<IReadOnlyList<EmployeeSkillDto>> GetSkillsAsync(int id, CancellationToken cancellationToken = default) =>
        _skillService.GetSkillsAsync(id, cancellationToken);

    public Task<EmployeeSkillDto> AddSkillAsync(int id, AddEmployeeSkillRequest request, int actingUserId, CancellationToken cancellationToken = default) =>
        _skillService.AddSkillAsync(id, request, actingUserId, cancellationToken);

    public Task<EmployeeSkillDto> UpdateSkillAsync(int id, int skillId, UpdateEmployeeSkillRequest request, int actingUserId, CancellationToken cancellationToken = default) =>
        _skillService.UpdateSkillAsync(id, skillId, request, actingUserId, cancellationToken);

    public Task RemoveSkillAsync(int id, int skillId, int actingUserId, CancellationToken cancellationToken = default) =>
        _skillService.RemoveSkillAsync(id, skillId, actingUserId, cancellationToken);

    public Task AssignManagerAsync(int id, AssignManagerRequest request, int actingUserId, CancellationToken cancellationToken = default) =>
        _commandService.AssignManagerAsync(id, request, actingUserId, cancellationToken);
}
