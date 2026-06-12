using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Employee;
using Prm.Application.Interfaces;
using Prm.Application.Validation;

namespace Prm.Application.Services.Employees;

public class EmployeeReminderService : IEmployeeReminderService
{
    private readonly IEmployeeContextService _context;
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly ITimesheetRepository _timesheets;
    private readonly ILogger<EmployeeReminderService> _logger;

    public EmployeeReminderService(
        IEmployeeContextService context,
        IResourceProfileRepository resourceProfiles,
        ITimesheetRepository timesheets,
        ILogger<EmployeeReminderService> logger)
    {
        _context = context;
        _resourceProfiles = resourceProfiles;
        _timesheets = timesheets;
        _logger = logger;
    }

    public async Task<EmployeeReminderResponse> GetReminderAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var employeeContext = await _context.ResolveAsync(userId, cancellationToken);
        var resourceProfile = EntityGuard.EnsureFound(
            await _resourceProfiles.GetByIdAsync(employeeContext.ResourceProfileId, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        var targetWeekStart = ActiveDateHelper.GetPreviousCompletedWeekStart();
        var hadAllocation = resourceProfile.Allocations
            .Any(allocation => ActiveDateHelper.IsAllocationActiveDuringWeek(allocation, targetWeekStart));

        if (!hadAllocation)
        {
            return new EmployeeReminderResponse(false, null, null);
        }

        var exists = await _timesheets.ExistsForResourceProfileWeekAsync(
            employeeContext.ResourceProfileId,
            targetWeekStart,
            cancellationToken);

        if (exists)
        {
            return new EmployeeReminderResponse(false, null, null);
        }

        _logger.LogInformation(
            "Timesheet reminder returned. ResourceProfileId={ResourceProfileId}, WeekStart={WeekStart}",
            employeeContext.ResourceProfileId,
            targetWeekStart);

        return new EmployeeReminderResponse(
            true,
            targetWeekStart,
            $"Timesheet for week {targetWeekStart:dd-MMM-yyyy} has not been submitted.");
    }
}
