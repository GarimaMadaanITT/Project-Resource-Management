using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Interfaces;
using Prm.Application.Services.Notifications;
using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Manager;

public class ManagerTeamTimesheetService : IManagerTeamTimesheetService
{
    private readonly IManagerContextService _context;
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly ITimesheetRepository _timesheets;
    private readonly ITimesheetComplianceRepository _complianceRepository;

    public ManagerTeamTimesheetService(
        IManagerContextService context,
        IResourceProfileRepository resourceProfiles,
        ITimesheetRepository timesheets,
        ITimesheetComplianceRepository complianceRepository)
    {
        _context = context;
        _resourceProfiles = resourceProfiles;
        _timesheets = timesheets;
        _complianceRepository = complianceRepository;
    }

    public async Task<TeamTimesheetsResponse> GetTeamTimesheetsAsync(
        int managerUserId,
        DateOnly? weekStart,
        CancellationToken cancellationToken = default)
    {
        var managerContext = await _context.ResolveAsync(managerUserId, cancellationToken);
        var team = await _resourceProfiles.GetTeamByManagerUserIdAsync(
            managerContext.ManagerUserId,
            activeOnly: true,
            cancellationToken);

        var resolvedWeekStart = weekStart ?? ActiveDateHelper.GetCurrentWeekStartUtc();
        var teamResourceProfileIds = team.Select(profile => profile.Id).ToList();

        var timesheets = await _timesheets.GetTeamTimesheetsByWeekAsync(
            teamResourceProfileIds,
            resolvedWeekStart,
            cancellationToken);

        var timesheetByProfile = timesheets.ToDictionary(timesheet => timesheet.ResourceProfileId);
        var today = ActiveDateHelper.TodayUtc;
        var compliances = await _complianceRepository.GetByResourceProfileIdsAndWeeksAsync(
            teamResourceProfileIds,
            [resolvedWeekStart],
            cancellationToken);
        var complianceByProfile = compliances.ToDictionary(compliance => compliance.ResourceProfileId);
        var rows = new List<TeamTimesheetRowDto>();

        foreach (var resourceProfile in team)
        {
            var weekAllocations = resourceProfile.Allocations
                .Where(allocation => ActiveDateHelper.IsAllocationActiveDuringWeek(allocation, resolvedWeekStart))
                .ToList();

            if (weekAllocations.Count == 0)
            {
                continue;
            }

            timesheetByProfile.TryGetValue(resourceProfile.Id, out var timesheet);
            var hasSubmittedTimesheet = timesheet is not null;
            complianceByProfile.TryGetValue(resourceProfile.Id, out var compliance);
            var status = TimesheetDisplayStatusHelper.Resolve(
                hasSubmittedTimesheet,
                compliance?.Status,
                resolvedWeekStart,
                today);

            foreach (var allocation in weekAllocations)
            {
                var entry = timesheet?.Entries.FirstOrDefault(e => e.ProjectId == allocation.ProjectId);
                var hours = entry?.Hours ?? 0;

                rows.Add(new TeamTimesheetRowDto(
                    resourceProfile.User.FullName,
                    allocation.Project.Name,
                    hours,
                    status,
                    resourceProfile.TimesheetSubmissionFrozen));
            }
        }

        return new TeamTimesheetsResponse(resolvedWeekStart, rows);
    }

    public async Task<ManagerEmployeeTimesheetDetailResponse> GetEmployeeTimesheetDetailAsync(
        int managerUserId,
        int employeeId,
        DateOnly? weekStart,
        CancellationToken cancellationToken = default)
    {
        var managerContext = await _context.ResolveAsync(managerUserId, cancellationToken);
        var resourceProfile = EntityGuard.EnsureFound(
            await _resourceProfiles.GetTeamMemberAsync(managerContext.ManagerUserId, employeeId, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        ManagerScopeGuard.EnsureEmployeeOnTeam(resourceProfile, managerContext.ManagerUserId, actingManagerUserId: managerUserId);

        var resolvedWeekStart = weekStart ?? ActiveDateHelper.GetCurrentWeekStartUtc();
        var timesheets = await _timesheets.GetTeamTimesheetsByWeekAsync(
            [resourceProfile.Id],
            resolvedWeekStart,
            cancellationToken);

        var timesheet = timesheets.FirstOrDefault();
        var hasSubmittedTimesheet = timesheet is not null;
        var compliance = await _complianceRepository.GetByResourceProfileAndWeekAsync(
            resourceProfile.Id,
            resolvedWeekStart,
            cancellationToken);
        var weekAllocations = resourceProfile.Allocations
            .Where(allocation => ActiveDateHelper.IsAllocationActiveDuringWeek(allocation, resolvedWeekStart))
            .ToList();

        var entries = weekAllocations
            .Select(allocation =>
            {
                var entry = timesheet?.Entries.FirstOrDefault(e => e.ProjectId == allocation.ProjectId);
                return new ManagerTimesheetEntryDetailDto(
                    allocation.Project.Name,
                    entry?.Hours ?? 0,
                    ParseActivityTags(entry?.ActivityTags));
            })
            .ToList();

        var status = TimesheetDisplayStatusHelper.Resolve(
            hasSubmittedTimesheet,
            compliance?.Status,
            resolvedWeekStart,
            ActiveDateHelper.TodayUtc);

        return new ManagerEmployeeTimesheetDetailResponse(
            resourceProfile.Id,
            resourceProfile.User.FullName,
            resolvedWeekStart,
            status,
            resourceProfile.TimesheetSubmissionFrozen,
            entries);
    }

    private static IReadOnlyList<string> ParseActivityTags(string? activityTags)
    {
        if (string.IsNullOrWhiteSpace(activityTags))
        {
            return Array.Empty<string>();
        }

        return activityTags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
