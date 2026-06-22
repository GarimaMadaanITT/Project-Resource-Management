using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Entities;

namespace Prm.Application.Services.Notifications;

public class AtRiskSkillSuggestionService
{
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly ITimesheetRepository _timesheets;
    private readonly ISystemSettingsRepository _settings;

    public AtRiskSkillSuggestionService(
        IResourceProfileRepository resourceProfiles,
        ITimesheetRepository timesheets,
        ISystemSettingsRepository settings)
    {
        _resourceProfiles = resourceProfiles;
        _timesheets = timesheets;
        _settings = settings;
    }

    public async Task<IReadOnlyList<string>> GetSuggestedEmployeesAsync(
        string requirement,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        var today = ActiveDateHelper.TodayUtc;
        var parsed = AiRequirementParser.Parse(requirement);
        var profiles = await _resourceProfiles.GetOrgWideCandidatesAsync(cancellationToken);

        var filtered = AiCapacityFilter.FilterTeam(
            profiles,
            settings.MaxWeeklyHours,
            parsed.HoursPerWeek,
            today);

        if (filtered.Count == 0)
        {
            return Array.Empty<string>();
        }

        var enriched = await EnrichWithRecentTagsAsync(filtered, cancellationToken);
        var ranked = AiSkillMatchRanker.RankMatches(requirement, enriched, Array.Empty<DTOs.Manager.SkillMatchResultItem>());

        return ranked
            .Select(match => $"{match.EmployeeName} — {match.Reason}")
            .ToList();
    }

    private async Task<IReadOnlyList<AiCapacityFilter.CandidateSnapshot>> EnrichWithRecentTagsAsync(
        IReadOnlyList<AiCapacityFilter.CandidateSnapshot> candidates,
        CancellationToken cancellationToken)
    {
        var enriched = new List<AiCapacityFilter.CandidateSnapshot>(candidates.Count);

        foreach (var candidate in candidates)
        {
            var recentTags = await _timesheets.GetRecentActivityTagsAsync(
                candidate.ResourceProfile.Id,
                cancellationToken: cancellationToken);

            enriched.Add(candidate with { RecentActivityTags = recentTags });
        }

        return enriched;
    }
}
