using Prm.Application.DTOs.Admin;

namespace Prm.Application.Interfaces;

public interface IAdminProjectService
{
    Task<IReadOnlyList<ProjectListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProjectListItemDto> CreateAsync(CreateProjectRequest request, int actingUserId, CancellationToken cancellationToken = default);
    Task<ProjectListItemDto> UpdateAsync(int id, UpdateProjectRequest request, int actingUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MilestoneDto>> GetMilestonesAsync(int projectId, CancellationToken cancellationToken = default);
    Task<MilestoneDto> AddMilestoneAsync(int projectId, AddMilestoneRequest request, int actingUserId, CancellationToken cancellationToken = default);
    Task<MilestoneDto> UpdateMilestoneStatusAsync(int projectId, int milestoneId, UpdateMilestoneStatusRequest request, int actingUserId, CancellationToken cancellationToken = default);
}
