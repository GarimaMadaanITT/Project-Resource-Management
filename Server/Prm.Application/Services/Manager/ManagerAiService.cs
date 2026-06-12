using Microsoft.Extensions.Logging;
using Prm.Application.Common;

using Prm.Application.DTOs.Manager;

using Prm.Application.Interfaces;

using Prm.Application.Validation;

using Prm.Domain.Enums;

using Prm.Domain.Exceptions;



namespace Prm.Application.Services.Manager;



public class ManagerAiService : IManagerAiService

{

    private const string SkillMatchSystemPrompt =

        """

        You are a resource planning assistant for an IT services company.

        Given a project requirement and a list of pre-filtered employee candidates,

        return ONLY valid JSON in this exact shape:

        {"matches":[{"employeeId":123,"reason":"Plain English reason referencing skills and availability"}]}

        Include up to 3 best matches ordered by fit. Use only employee IDs from the candidate list.

        Do not include markdown or extra text outside the JSON object.

        """;



    private const string RiskSummarySystemPrompt =

        """

        You are a delivery manager assistant. Given factual project data (milestones, allocations, timesheets, risk flags),

        write a concise plain-English paragraph (3-5 sentences) summarizing risks and concerns.

        Be specific, actionable, and professional. Do not invent facts not present in the input.

        Return only the paragraph text without JSON or bullet lists.

        """;



    private const string AiDisclaimer =

        "Note: Suggestions are AI-generated. Verify skills and availability before confirming allocations.";



    private const string RiskDisclaimer =

        "Note: This summary is AI-generated from milestone and timesheet data.";



    private readonly IManagerContextService _context;

    private readonly IProjectRepository _projects;

    private readonly IResourceProfileRepository _resourceProfiles;

    private readonly ITimesheetRepository _timesheets;

    private readonly ISystemSettingsRepository _settings;

    private readonly ILlmCompletionService _llm;

    private readonly ILogger<ManagerAiService> _logger;



    public ManagerAiService(

        IManagerContextService context,

        IProjectRepository projects,

        IResourceProfileRepository resourceProfiles,

        ITimesheetRepository timesheets,

        ISystemSettingsRepository settings,

        ILlmCompletionService llm,

        ILogger<ManagerAiService> logger)

    {

        _context = context;

        _projects = projects;

        _resourceProfiles = resourceProfiles;

        _timesheets = timesheets;

        _settings = settings;

        _llm = llm;

        _logger = logger;

    }



    public async Task<SkillMatchResponse> SkillMatchAsync(

        int managerUserId,

        SkillMatchRequest request,

        CancellationToken cancellationToken = default)

    {

        StringGuard.RequireNonEmpty(request.Requirement, "Requirement");



        var managerContext = await _context.ResolveAsync(managerUserId, cancellationToken);

        var project = EntityGuard.EnsureFound(

            await _projects.GetByIdWithAllocationsAsync(request.ProjectId, cancellationToken),

            ErrorMessages.ProjectNotFound);

        ManagerScopeGuard.EnsureProjectOwnedByManager(project, managerUserId);



        var parsed = AiRequirementParser.Parse(request.Requirement);

        var settings = await _settings.GetAsync(cancellationToken);

        var today = ActiveDateHelper.TodayUtc;



        var team = await _resourceProfiles.GetTeamByManagerUserIdAsync(

            managerContext.ManagerUserId,

            activeOnly: true,

            cancellationToken);



        var filtered = AiCapacityFilter.FilterTeam(

            team,

            settings.MaxWeeklyHours,

            parsed.HoursPerWeek,

            today);



        if (filtered.Count == 0)

        {

            var requiredText = parsed.HoursPerWeek.HasValue

                ? $"{parsed.HoursPerWeek} hrs/week"

                : "enough free capacity for this request";



            throw new DomainException(

                $"No team members have {requiredText}. Try lowering the hours requirement or ending an allocation first.");

        }



        var enriched = await EnrichWithRecentTagsAsync(filtered, cancellationToken);

        var candidatesById = enriched.ToDictionary(candidate => candidate.ResourceProfile.Id);



        var userPrompt = AiMatchResponseParser.BuildSkillMatchPrompt(

            project.Name,

            request.Requirement.Trim(),

            enriched);



        var llmResult = await _llm.CompleteAsync(SkillMatchSystemPrompt, userPrompt, cancellationToken);

        var matches = AiMatchResponseParser.Parse(llmResult.Text, candidatesById);



        return new SkillMatchResponse(

            project.Name,

            request.Requirement.Trim(),

            parsed.HoursPerWeek,

            enriched.Count,

            matches,

            AiDisclaimer,

            llmResult.UsedFallbackProvider);

    }



    public async Task<TeamBuilderResponse> TeamBuilderAsync(

        int managerUserId,

        TeamBuilderRequest request,

        CancellationToken cancellationToken = default)

    {

        var requirement = TeamBuilderRequirementValidator.Validate(request.Requirement);

        await _context.ResolveAsync(managerUserId, cancellationToken);



        var today = ActiveDateHelper.TodayUtc;

        var profiles = await _resourceProfiles.GetOrgWideCandidatesAsync(cancellationToken);

        var allCandidates = AiTeamBuilderCandidateMapper.MapAll(profiles, today);

        var assignableCandidates = allCandidates

            .Where(AiTeamBuilderCandidateMapper.IsFullyBenched)

            .ToList();



        var systemPrompt = AiTeamBuilderPromptBuilder.BuildSystemPrompt();

        var userPrompt = AiTeamBuilderPromptBuilder.BuildUserPrompt(

            requirement,

            assignableCandidates,

            allCandidates);



        var llmResult = await _llm.CompleteAsync(systemPrompt, userPrompt, cancellationToken);

        IReadOnlyList<TeamBuilderRoleResultDto> roles;
        if (llmResult.UsedFallbackProvider)
        {
            roles = AiTeamBuilderRequirementAnalyzer.ParseRequirementToRoles(requirement);
        }
        else
        {
            roles = AiTeamBuilderResponseParser.Parse(llmResult.Text);
        }

        roles = AiTeamBuilderSkillRanker.EnrichAndCorrectRoles(roles, assignableCandidates, allCandidates);

        LogCandidatePool(managerUserId, requirement, allCandidates, assignableCandidates);

        TeamBuilderResponseValidator.Validate(roles, assignableCandidates);



        var filledCount = roles.Count(role => role.Status == TeamBuilderConstants.StatusFilled);

        var gapCount = roles.Count - filledCount;

        _logger.LogInformation(

            "Team builder completed. ManagerUserId={ManagerUserId}, RoleCount={RoleCount}, Filled={FilledCount}, Gap={GapCount}, UsedFallback={UsedFallback}",

            managerUserId,

            roles.Count,

            filledCount,

            gapCount,

            llmResult.UsedFallbackProvider);



        return new TeamBuilderResponse(

            requirement,

            allCandidates.Count,

            assignableCandidates.Count,

            roles,

            TeamBuilderConstants.TeamBuilderDisclaimer,

            llmResult.UsedFallbackProvider);

    }



    public async Task<RiskSummaryResponse> GetRiskSummaryAsync(

        int managerUserId,

        int projectId,

        CancellationToken cancellationToken = default)

    {

        await _context.ResolveAsync(managerUserId, cancellationToken);



        var project = EntityGuard.EnsureFound(

            await _projects.GetByIdWithAllocationsAsync(projectId, cancellationToken),

            ErrorMessages.ProjectNotFound);

        ManagerScopeGuard.EnsureProjectOwnedByManager(project, managerUserId);



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



        var riskFlags = ProjectRiskFlagCalculator.Calculate(

            project,

            projectActiveAllocations,

            resourceProfileTotalUtilisation,

            recentEntries,

            settings.MaxWeeklyHours,

            today);



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

        if (string.IsNullOrWhiteSpace(summary))

        {

            summary = string.Join(" ", riskFlagMessages);

        }



        return new RiskSummaryResponse(

            project.Id,

            project.Name,

            project.HealthStatus.ToString(),

            summary,

            RiskDisclaimer,

            llmResult.UsedFallbackProvider);

    }



    private void LogCandidatePool(

        int managerUserId,

        string requirement,

        IReadOnlyList<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> allCandidates,

        IReadOnlyList<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> assignableCandidates)

    {

        _logger.LogInformation(

            "Team builder candidate pool. ManagerUserId={ManagerUserId}, Requirement={Requirement}, Total={Total}, Benched={Benched}",

            managerUserId,

            requirement,

            allCandidates.Count,

            assignableCandidates.Count);



        foreach (var candidate in allCandidates)

        {

            _logger.LogInformation(

                "Team builder candidate. ResourceProfileId={ResourceProfileId}, UserId={UserId}, Name={Name}, Util={UtilisationPercent}%, Skills={Skills}, Benched={IsBenched}",

                candidate.EmployeeId,

                candidate.UserId,

                candidate.FullName,

                candidate.UtilisationPercent,

                string.Join(", ", candidate.Skills.Select(skill => $"{skill.Name}({skill.Proficiency})")),

                AiTeamBuilderCandidateMapper.IsFullyBenched(candidate));

        }

    }



    private async Task<IReadOnlyList<AiCapacityFilter.CandidateSnapshot>> EnrichWithRecentTagsAsync(

        IReadOnlyList<AiCapacityFilter.CandidateSnapshot> candidates,

        CancellationToken cancellationToken)

    {

        var enriched = new List<AiCapacityFilter.CandidateSnapshot>(candidates.Count);

        foreach (var candidate in candidates)

        {

            var tags = await _timesheets.GetRecentActivityTagsAsync(

                candidate.ResourceProfile.Id,

                ValidationConstants.DefaultRecentActivityWeeks,

                cancellationToken);



            enriched.Add(candidate with { RecentActivityTags = tags });

        }



        return enriched;

    }

}


