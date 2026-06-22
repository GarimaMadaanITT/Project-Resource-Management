using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Notifications;

public interface IProjectRiskContentBuilder
{
    Task<string> BuildSummaryAsync(
        Project project,
        IReadOnlyList<ProjectRiskFlagCalculator.RiskFlag> riskFlags,
        CancellationToken cancellationToken = default);
}

public class ProjectRiskContentBuilder : IProjectRiskContentBuilder
{
    private const string RiskSummarySystemPrompt =
        """
        You are a delivery manager assistant. Given factual project data (milestones, allocations, timesheets, risk flags),
        write a concise plain-English paragraph (3-5 sentences) summarizing risks and concerns.
        Do not invent facts not present in the input. Do not use markdown.
        """;

    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly ITimesheetRepository _timesheets;
    private readonly ISystemSettingsRepository _settings;
    private readonly ILlmCompletionService _llm;

    public ProjectRiskContentBuilder(
        IResourceProfileRepository resourceProfiles,
        ITimesheetRepository timesheets,
        ISystemSettingsRepository settings,
        ILlmCompletionService llm)
    {
        _resourceProfiles = resourceProfiles;
        _timesheets = timesheets;
        _settings = settings;
        _llm = llm;
    }

    public async Task<string> BuildSummaryAsync(
        Project project,
        IReadOnlyList<ProjectRiskFlagCalculator.RiskFlag> riskFlags,
        CancellationToken cancellationToken = default)
    {
        var today = ActiveDateHelper.TodayUtc;
        var settings = await _settings.GetAsync(cancellationToken);

        var projectActiveAllocations = project.Allocations
            .Where(allocation => ActiveDateHelper.IsAllocationActive(allocation, today))
            .ToList();

        var resourceProfileIds = projectActiveAllocations
            .Select(allocation => allocation.ResourceProfileId)
            .Distinct()
            .ToList();

        var resourceProfileTotalUtilisation = new Dictionary<int, int>();
        foreach (var resourceProfileId in resourceProfileIds)
        {
            var resourceProfile = await _resourceProfiles.GetByIdAsync(resourceProfileId, cancellationToken);
            if (resourceProfile is null)
            {
                continue;
            }

            resourceProfileTotalUtilisation[resourceProfileId] =
                ActiveDateHelper.SumActiveUtilisation(resourceProfile.Allocations, today);
        }

        var lastWeekStart = ActiveDateHelper.GetCurrentWeekStartUtc().AddDays(-7);
        var recentEntries = await _timesheets.GetEntriesForResourceProfilesAndWeekAsync(
            resourceProfileIds,
            lastWeekStart,
            cancellationToken);

        var milestoneFacts = project.Milestones
            .OrderBy(milestone => milestone.DueDate)
            .Select(milestone =>
            {
                var overdue = milestone.Status != MilestoneStatus.Done && milestone.DueDate < today
                    ? " (OVERDUE)"
                    : string.Empty;
                return $"{milestone.Title}: due {milestone.DueDate:yyyy-MM-dd}, status {milestone.Status}{overdue}";
            })
            .ToList();

        var allocationFacts = projectActiveAllocations
            .Select(allocation =>
                $"{allocation.ResourceProfile.User.FullName}: {allocation.UtilisationPercent}% from {allocation.FromDate:yyyy-MM-dd} to {allocation.ToDate:yyyy-MM-dd}")
            .ToList();

        var resourceNames = projectActiveAllocations
            .GroupBy(allocation => allocation.ResourceProfileId)
            .ToDictionary(
                group => group.Key,
                group => group.First().ResourceProfile.User.FullName);

        var timesheetFacts = recentEntries
            .GroupBy(entry => entry.Timesheet.ResourceProfileId)
            .Select(group =>
            {
                var hours = group.Sum(entry => entry.Hours);
                var name = resourceNames.TryGetValue(group.Key, out var resourceName)
                    ? resourceName
                    : $"Employee {group.Key}";
                return $"{name} logged {hours} hrs on this project last week";
            })
            .ToList();

        if (timesheetFacts.Count == 0)
        {
            timesheetFacts.Add("No hours logged on this project last week.");
        }

        var riskFlagMessages = riskFlags
            .Where(flag => flag.IsRisk)
            .Select(flag => flag.Message)
            .ToList();

        if (riskFlagMessages.Count == 0)
        {
            riskFlagMessages.Add("No critical risk flags detected from milestone and timesheet data.");
        }

        var userPrompt = AiMatchResponseParser.BuildRiskSummaryPrompt(
            project.Name,
            project.HealthStatus.ToString(),
            project.EndDate,
            milestoneFacts,
            allocationFacts,
            timesheetFacts,
            riskFlagMessages);

        var llmResult = await _llm.CompleteAsync(RiskSummarySystemPrompt, userPrompt, cancellationToken);
        var summary = llmResult.Text.Trim();

        return string.IsNullOrWhiteSpace(summary)
            ? string.Join(" ", riskFlagMessages)
            : summary;
    }
}
