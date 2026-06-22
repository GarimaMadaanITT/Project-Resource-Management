using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Employee;
using Prm.Application.Interfaces;
using Prm.Application.Services.Notifications;
using Prm.Application.Validation;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Application.Services.Employees;

public class EmployeeTimesheetService : IEmployeeTimesheetService
{
    private readonly IEmployeeContextService _context;
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly ITimesheetRepository _timesheets;
    private readonly ITimesheetComplianceRepository _complianceRepository;
    private readonly ISystemSettingsRepository _settings;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<EmployeeTimesheetService> _logger;

    public EmployeeTimesheetService(
        IEmployeeContextService context,
        IResourceProfileRepository resourceProfiles,
        ITimesheetRepository timesheets,
        ITimesheetComplianceRepository complianceRepository,
        ISystemSettingsRepository settings,
        IAuditLogService auditLog,
        ILogger<EmployeeTimesheetService> logger)
    {
        _context = context;
        _resourceProfiles = resourceProfiles;
        _timesheets = timesheets;
        _complianceRepository = complianceRepository;
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
        var resourceProfile = EntityGuard.EnsureFound(
            await _resourceProfiles.GetByIdAsync(employeeContext.ResourceProfileId, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        if (resourceProfile.TimesheetSubmissionFrozen)
        {
            throw new DomainException(ErrorMessages.TimesheetSubmissionFrozen);
        }

        var weekStart = ActiveDateHelper.ResolveWeekStart(request.WeekStart);
        var settings = await _settings.GetAsync(cancellationToken);
        var timesheetExists = await _timesheets.ExistsForResourceProfileWeekAsync(
            employeeContext.ResourceProfileId,
            weekStart,
            cancellationToken);

        var weekAllocations = resourceProfile.Allocations
            .Where(allocation => ActiveDateHelper.IsAllocationActiveDuringWeek(allocation, weekStart))
            .ToList();

        TimesheetValidator.ValidateSubmitRequest(
            request,
            weekStart,
            weekAllocations,
            settings.MaxWeeklyHours,
            timesheetExists,
            _logger);

        var timesheet = TimesheetBuilder.Build(employeeContext.ResourceProfileId, weekStart, request.Entries);
        await _timesheets.AddAsync(timesheet, cancellationToken);

        var compliance = await _complianceRepository.GetByResourceProfileAndWeekAsync(
            employeeContext.ResourceProfileId,
            weekStart,
            cancellationToken);

        if (compliance is not null)
        {
            await _complianceRepository.DeleteAsync(compliance, cancellationToken);
        }

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
            "Timesheet submitted. ResourceProfileId={ResourceProfileId}, WeekStart={WeekStart}, TotalHours={TotalHours}, ProjectCount={ProjectCount}",
            employeeContext.ResourceProfileId,
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
        var resourceProfile = EntityGuard.EnsureFound(
            await _resourceProfiles.GetByIdAsync(employeeContext.ResourceProfileId, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        var existingTimesheets = await _timesheets.GetByResourceProfileIdAsync(employeeContext.ResourceProfileId, cancellationToken);
        var timesheetByWeek = existingTimesheets.ToDictionary(timesheet => timesheet.WeekStart);

        var currentWeekStart = ActiveDateHelper.GetCurrentWeekStartUtc();
        var today = ActiveDateHelper.TodayUtc;
        var weeks = new List<TimesheetListItemDto>();
        var weekStarts = Enumerable.Range(0, ValidationConstants.TimesheetHistoryWeeks)
            .Select(index => currentWeekStart.AddDays(-7 * index))
            .ToList();

        var compliances = await _complianceRepository.GetByResourceProfileIdsAndWeeksAsync(
            [employeeContext.ResourceProfileId],
            weekStarts,
            cancellationToken);

        var complianceByWeek = compliances.ToDictionary(compliance => compliance.WeekStart);

        for (var index = 0; index < ValidationConstants.TimesheetHistoryWeeks; index++)
        {
            var weekStart = weekStarts[index];
            var hadAllocation = resourceProfile.Allocations
                .Any(allocation => ActiveDateHelper.IsAllocationActiveDuringWeek(allocation, weekStart));

            if (!hadAllocation)
            {
                continue;
            }

            complianceByWeek.TryGetValue(weekStart, out var compliance);
            var hasSubmitted = timesheetByWeek.ContainsKey(weekStart);

            weeks.Add(new TimesheetListItemDto(
                weekStart,
                hasSubmitted ? timesheetByWeek[weekStart].TotalHours : 0,
                TimesheetDisplayStatusHelper.Resolve(
                    hasSubmitted,
                    compliance?.Status,
                    weekStart,
                    today)));
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
        var timesheet = await _timesheets.GetByResourceProfileAndWeekAsync(
            employeeContext.ResourceProfileId,
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
