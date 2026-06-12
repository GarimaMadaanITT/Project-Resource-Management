using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Employee;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Employees;

public class EmployeeTimesheetService : IEmployeeTimesheetService
{
    private readonly IEmployeeContextService _context;
    private readonly IEmployeeRepository _employees;
    private readonly ITimesheetRepository _timesheets;
    private readonly ISystemSettingsRepository _settings;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<EmployeeTimesheetService> _logger;

    public EmployeeTimesheetService(
        IEmployeeContextService context,
        IEmployeeRepository employees,
        ITimesheetRepository timesheets,
        ISystemSettingsRepository settings,
        IAuditLogService auditLog,
        ILogger<EmployeeTimesheetService> logger)
    {
        _context = context;
        _employees = employees;
        _timesheets = timesheets;
        _settings = settings;
        _auditLog = auditLog;
        _logger = logger;
    }

    public ActivityTagsResponse GetActivityTags() =>
        new(ActivityTagCatalog.PredefinedTags, ActivityTagCatalog.AllowsCustomOther);

    public async Task<SubmitTimesheetResponse> SubmitAsync(
        int userId,
        SubmitTimesheetRequest request,
        CancellationToken cancellationToken = default)
    {
        var employeeContext = await _context.ResolveAsync(userId, cancellationToken);
        var employee = EntityGuard.EnsureFound(
            await _employees.GetByIdAsync(employeeContext.EmployeeId, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        var weekStart = ActiveDateHelper.ResolveWeekStart(request.WeekStart);
        var settings = await _settings.GetAsync(cancellationToken);
        var timesheetExists = await _timesheets.ExistsForEmployeeWeekAsync(
            employeeContext.EmployeeId,
            weekStart,
            cancellationToken);

        var weekAllocations = employee.Allocations
            .Where(allocation => ActiveDateHelper.IsAllocationActiveDuringWeek(allocation, weekStart))
            .ToList();

        TimesheetValidator.ValidateSubmitRequest(
            request,
            weekStart,
            weekAllocations,
            settings.MaxWeeklyHours,
            timesheetExists,
            _logger);

        var timesheet = TimesheetBuilder.Build(employeeContext.EmployeeId, weekStart, request.Entries);
        await _timesheets.AddAsync(timesheet, cancellationToken);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.Timesheet,
            timesheet.Id,
            AuditConstants.Actions.Submitted,
            null,
            AuditSnapshotBuilder.TimesheetSnapshot(timesheet),
            userId,
            AuthConstants.RoleName(UserRole.Employee),
            AuditConstants.Sources.User,
            cancellationToken);

        _logger.LogInformation(
            "Timesheet submitted. EmployeeId={EmployeeId}, WeekStart={WeekStart}, TotalHours={TotalHours}, ProjectCount={ProjectCount}",
            employeeContext.EmployeeId,
            weekStart,
            timesheet.TotalHours,
            request.Entries.Count);

        return new SubmitTimesheetResponse(
            weekStart,
            timesheet.TotalHours,
            TimesheetStatus.Submitted.ToString(),
            "Timesheet submitted successfully.");
    }

    public async Task<MyTimesheetsResponse> GetMyTimesheetsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var employeeContext = await _context.ResolveAsync(userId, cancellationToken);
        var employee = EntityGuard.EnsureFound(
            await _employees.GetByIdAsync(employeeContext.EmployeeId, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        var existingTimesheets = await _timesheets.GetByEmployeeIdAsync(employeeContext.EmployeeId, cancellationToken);
        var timesheetByWeek = existingTimesheets.ToDictionary(timesheet => timesheet.WeekStart);

        var currentWeekStart = ActiveDateHelper.GetCurrentWeekStartUtc();
        var weeks = new List<TimesheetListItemDto>();

        for (var index = 0; index < ValidationConstants.TimesheetHistoryWeeks; index++)
        {
            var weekStart = currentWeekStart.AddDays(-7 * index);
            var hadAllocation = employee.Allocations
                .Any(allocation => ActiveDateHelper.IsAllocationActiveDuringWeek(allocation, weekStart));

            if (!hadAllocation)
            {
                continue;
            }

            if (timesheetByWeek.TryGetValue(weekStart, out var timesheet))
            {
                weeks.Add(new TimesheetListItemDto(
                    weekStart,
                    timesheet.TotalHours,
                    TimesheetStatus.Submitted.ToString()));
            }
            else
            {
                weeks.Add(new TimesheetListItemDto(
                    weekStart,
                    0,
                    TimesheetStatus.Missed.ToString()));
            }
        }

        return new MyTimesheetsResponse(weeks);
    }

    public async Task<TimesheetWeekDetailDto> GetWeekDetailAsync(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        ActiveDateHelper.EnsureMondayWeekStart(weekStart);

        var employeeContext = await _context.ResolveAsync(userId, cancellationToken);
        var timesheet = await _timesheets.GetByEmployeeAndWeekAsync(
            employeeContext.EmployeeId,
            weekStart,
            cancellationToken);

        EntityGuard.EnsureFound(timesheet, ErrorMessages.TimesheetNotFound);

        var entries = timesheet!.Entries
            .Select(entry => new TimesheetEntryDetailDto(
                entry.Project.Name,
                entry.Hours,
                ParseActivityTags(entry.ActivityTags)))
            .ToList();

        return new TimesheetWeekDetailDto(
            timesheet.WeekStart,
            timesheet.TotalHours,
            TimesheetStatus.Submitted.ToString(),
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
