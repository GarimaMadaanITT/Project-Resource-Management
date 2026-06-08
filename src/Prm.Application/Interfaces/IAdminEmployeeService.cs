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
