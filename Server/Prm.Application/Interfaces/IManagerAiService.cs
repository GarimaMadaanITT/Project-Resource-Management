using Prm.Application.DTOs.Manager;

namespace Prm.Application.Interfaces;

public interface IManagerAiService
{
    Task<SkillMatchResponse> SkillMatchAsync(
        int managerUserId,
        SkillMatchRequest request,
        CancellationToken cancellationToken = default);

    Task<RiskSummaryResponse> GetRiskSummaryAsync(
        int managerUserId,
        int projectId,
        CancellationToken cancellationToken = default);

    Task<TeamBuilderResponse> TeamBuilderAsync(
        int managerUserId,
        TeamBuilderRequest request,
        CancellationToken cancellationToken = default);
}
