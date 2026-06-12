using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Manager;

public class ManagerProjectService : IManagerProjectService
{
    private readonly IManagerContextService _context;
    private readonly IProjectRepository _projects;
    private readonly IAllocationRepository _allocations;
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly ITimesheetRepository _timesheets;
    private readonly ISystemSettingsRepository _settings;

    public ManagerProjectService(
        IManagerContextService context,
        IProjectRepository projects,
        IAllocationRepository allocations,
        IResourceProfileRepository resourceProfiles,
        ITimesheetRepository timesheets,
        ISystemSettingsRepository settings)
    {
        _context = context;
        _projects = projects;
        _allocations = allocations;
        _resourceProfiles = resourceProfiles;
        _timesheets = timesheets;
        _settings = settings;
    }

    public async Task<IReadOnlyList<ManagerProjectListItemDto>> GetProjectsAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        await _context.ResolveAsync(managerUserId, cancellationToken);
        var projects = await _projects.GetByManagerUserIdAsync(managerUserId, cancellationToken);

        return projects
            .Select(project => new ManagerProjectListItemDto(
                project.Id,
                project.Name,
                project.EndDate,
                project.HealthStatus.ToString()))
            .ToList();
    }

    public async Task<ManagerProjectDetailDto> GetProjectDetailAsync(
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

        var settings = await _settings.GetAsync(cancellationToken);
        var riskFlags = ProjectRiskFlagCalculator.Calculate(
            project,
            projectActiveAllocations,
            resourceProfileTotalUtilisation,
            recentEntries,
            settings.MaxWeeklyHours,
            today);

        var milestones = project.Milestones
            .OrderBy(milestone => milestone.DueDate)
            .Select(milestone => new ManagerMilestoneDto(
                milestone.Id,
                milestone.Title,
                milestone.DueDate,
                milestone.Status.ToString(),
                milestone.Status != MilestoneStatus.Done && milestone.DueDate < today))
            .ToList();

        var allocations = projectActiveAllocations
            .Select(allocation => new ManagerProjectAllocationDto(
                allocation.ResourceProfile.User.FullName,
                allocation.UtilisationPercent,
                allocation.FromDate,
                allocation.ToDate))
            .ToList();

        return new ManagerProjectDetailDto(
            project.Id,
            project.Name,
            project.Description,
            project.EndDate,
            project.HealthStatus.ToString(),
            riskFlags.Select(flag => new ManagerRiskFlagDto(flag.Code, flag.Message, flag.IsRisk)).ToList(),
            milestones,
            allocations);
    }
}
