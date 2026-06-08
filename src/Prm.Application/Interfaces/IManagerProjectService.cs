using Prm.Application.DTOs.Manager;

namespace Prm.Application.Interfaces;

public interface IManagerProjectService
{
    Task<IReadOnlyList<ManagerProjectListItemDto>> GetProjectsAsync(
        int managerUserId,
        CancellationToken cancellationToken = default);
    Task<ManagerProjectDetailDto> GetProjectDetailAsync(
        int managerUserId,
        int projectId,
        CancellationToken cancellationToken = default);
}
