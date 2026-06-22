using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Entities;

namespace Prm.Application.Services.Manager;

public class ManagerDashboardService : IManagerDashboardService
{
    private readonly IManagerContextService _context;
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly ITimesheetRepository _timesheets;

    public ManagerDashboardService(
        IManagerContextService context,
        IResourceProfileRepository resourceProfiles,
        ITimesheetRepository timesheets)
    {
        _context = context;
        _resourceProfiles = resourceProfiles;
        _timesheets = timesheets;
    }

    public async Task<ManagerDashboardResponse> GetDashboardAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        var managerContext = await _context.ResolveAsync(managerUserId, cancellationToken);
        var team = await _resourceProfiles.GetTeamByManagerUserIdAsync(
            managerContext.ManagerUserId,
            activeOnly: true,
            cancellationToken);

        var today = ActiveDateHelper.TodayUtc;
        var bench = new List<ManagerDashboardEmployeeDto>();
        var partial = new List<ManagerDashboardEmployeeDto>();
        var full = new List<ManagerDashboardEmployeeDto>();

        foreach (var resourceProfile in team)
        {
            var utilisation = ActiveDateHelper.SumActiveUtilisation(resourceProfile.Allocations, today);
            var dto = MapEmployee(resourceProfile, utilisation);

            if (utilisation == 0)
            {
                bench.Add(dto);
            }
            else if (utilisation >= ValidationConstants.MaxUtilisationPercent)
            {
                full.Add(dto);
            }
            else
            {
                partial.Add(dto);
            }
        }

        return new ManagerDashboardResponse(
            bench,
            partial,
            full,
            bench.Count,
            partial.Count,
            full.Count);
    }

    public async Task<ManagerEmployeeDetailDto> GetEmployeeDetailAsync(
        int managerUserId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var managerContext = await _context.ResolveAsync(managerUserId, cancellationToken);
        var resourceProfile = EntityGuard.EnsureFound(
            await _resourceProfiles.GetTeamMemberAsync(managerContext.ManagerUserId, employeeId, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        var today = ActiveDateHelper.TodayUtc;
        var utilisation = ActiveDateHelper.SumActiveUtilisation(resourceProfile.Allocations, today);
        var activeAllocations = resourceProfile.Allocations
            .Where(allocation => ActiveDateHelper.IsAllocationActive(allocation, today))
            .Select(allocation => new ManagerAllocationItemDto(
                allocation.Project.Name,
                allocation.UtilisationPercent,
                allocation.FromDate,
                allocation.ToDate))
            .ToList();

        var recentTags = await _timesheets.GetRecentActivityTagsAsync(
            resourceProfile.Id,
            ValidationConstants.DefaultRecentActivityWeeks,
            cancellationToken);

        return new ManagerEmployeeDetailDto(
            resourceProfile.Id,
            resourceProfile.User.FullName,
            resourceProfile.User.Department?.ToString() ?? string.Empty,
            EmployeeStatusResolver.ResolveStatusName(utilisation),
            utilisation,
            resourceProfile.User.Skills.Select(skill => skill.Skill.Name).ToList(),
            activeAllocations,
            recentTags);
    }

    private static ManagerDashboardEmployeeDto MapEmployee(ResourceProfile resourceProfile, int utilisation)
    {
        var skills = string.Join(", ", resourceProfile.User.Skills.Select(skill => skill.Skill.Name));
        var availability = ValidationConstants.MaxUtilisationPercent - utilisation;

        return new ManagerDashboardEmployeeDto(
            resourceProfile.Id,
            resourceProfile.User.FullName,
            resourceProfile.User.Department?.ToString() ?? string.Empty,
            utilisation,
            availability,
            skills);
    }
}
