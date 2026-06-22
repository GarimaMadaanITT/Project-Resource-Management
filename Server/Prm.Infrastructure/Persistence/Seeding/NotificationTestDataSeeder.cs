using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Infrastructure.Persistence.Seeding;

public class NotificationTestDataSeeder
{
    private const string AtRiskMilestoneTitle = "Email Test — Overdue Milestone";
    private const string EmployeeRoleName = "Employee";

    private readonly PrmDbContext _context;
    private readonly ILogger<NotificationTestDataSeeder> _logger;
    private readonly HashSet<int> _usedProfileIds = new();

    public NotificationTestDataSeeder(PrmDbContext context, ILogger<NotificationTestDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NotificationTestDataSeedResponse> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await _context.Users.AnyAsync(cancellationToken))
        {
            _logger.LogWarning("Notification test data skipped — no users found. Run main seed first.");
            return new NotificationTestDataSeedResponse(
                "Skipped — database has no users.",
                ActiveDateHelper.GetPreviousCompletedWeekStart(),
                Array.Empty<string>());
        }

        _usedProfileIds.Clear();

        var today = ActiveDateHelper.TodayUtc;
        var previousWeekStart = ActiveDateHelper.GetPreviousCompletedWeekStart();
        var allocationThrough = today.AddMonths(3);
        var preparedEmployees = new List<string>();

        _logger.LogInformation(
            "Seeding notification test data. Today={Today}, PreviousWeekStart={PreviousWeekStart}",
            today,
            previousWeekStart);

        await ClearAllTimesheetNotificationLogsAsync(cancellationToken);

        var employeeProfiles = await GetActiveEmployeeProfilesAsync(cancellationToken);

        if (await PrepareRaviForFreezeDemoAsync(
                preparedEmployees,
                employeeProfiles,
                previousWeekStart,
                allocationThrough,
                cancellationToken))
        {
            _logger.LogInformation("Prepared Ravi for Reminder 2 + freeze demo.");
        }

        if (await PrepareReminderScenarioForPreferredAsync(
                preparedEmployees,
                ["dev.patel"],
                employeeProfiles,
                previousWeekStart,
                allocationThrough,
                reminderCount: 0,
                cancellationToken))
        {
            _logger.LogInformation("Prepared Reminder 1 scenario.");
        }

        if (await PrepareReminderScenarioForPreferredAsync(
                preparedEmployees,
                ["sara.khan", "dev.patel"],
                employeeProfiles,
                previousWeekStart,
                allocationThrough,
                reminderCount: 1,
                cancellationToken))
        {
            _logger.LogInformation("Prepared Reminder 2 scenario.");
        }

        if (await PrepareFreezeScenarioForPreferredAsync(
                preparedEmployees,
                ["anil.mehta"],
                employeeProfiles,
                previousWeekStart,
                allocationThrough,
                cancellationToken))
        {
            _logger.LogInformation("Prepared Freeze scenario.");
        }

        await PrepareFrozenRestoreDemoUserAsync(previousWeekStart, allocationThrough, cancellationToken);
        await PrepareAtRiskEmailScenarioAsync(today, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var message =
            $"""
             Notification test data ready for week {previousWeekStart:dd-MMM-yyyy}.
             Ravi (ravi.kumar): ReminderCount=1 — use force-timesheet-compliance to send Reminder 2 + freeze.
             Reminder 1 (Mon+): dev.patel / any active employee.
             Reminder 2 (Tue+): sara.khan / dev.patel / next active employee (ReminderCount=1).
             Freeze (Wed+): anil.mehta / next active employee (ReminderCount=2).
             Frozen restore demo: frozen.test / Employee@1234.
             At-Risk: Beta CRM reset to OnTrack with overdue milestone.
             Call POST /api/admin/scheduler/run-now to trigger emails immediately.
             """;

        _logger.LogInformation(message);

        return new NotificationTestDataSeedResponse(message, previousWeekStart, preparedEmployees);
    }

    private async Task<bool> PrepareRaviForFreezeDemoAsync(
        List<string> preparedEmployees,
        IReadOnlyList<ResourceProfile> employeeProfiles,
        DateOnly previousWeekStart,
        DateOnly allocationThrough,
        CancellationToken cancellationToken)
    {
        var profile = employeeProfiles.FirstOrDefault(resourceProfile =>
                resourceProfile.User.Username.Equals("ravi.kumar", StringComparison.OrdinalIgnoreCase))
            ?? await LoadProfileAsync("ravi.kumar", cancellationToken);

        if (profile is null)
        {
            _logger.LogWarning("ravi.kumar not found — skipping freeze demo setup.");
            return false;
        }

        _usedProfileIds.Add(profile.Id);

        await PrepareTimesheetReminderForProfileAsync(
            profile,
            previousWeekStart,
            allocationThrough,
            reminderCount: 1,
            cancellationToken);

        preparedEmployees.Add(
            $"{profile.User.Username} ({profile.User.Email}) — ReminderCount=1 (force-timesheet-compliance → R2 + freeze)");
        return true;
    }

    private async Task<bool> PrepareReminderScenarioForPreferredAsync(
        List<string> preparedEmployees,
        IReadOnlyList<string> preferredUsernames,
        IReadOnlyList<ResourceProfile> employeeProfiles,
        DateOnly previousWeekStart,
        DateOnly allocationThrough,
        int reminderCount,
        CancellationToken cancellationToken)
    {
        var profile = await ResolveProfileAsync(preferredUsernames, employeeProfiles, cancellationToken);
        if (profile is null)
        {
            _logger.LogWarning(
                "No employee profile found for reminder scenario (ReminderCount={ReminderCount}). Preferred: {Usernames}",
                reminderCount,
                string.Join(", ", preferredUsernames));
            return false;
        }

        await PrepareTimesheetReminderForProfileAsync(
            profile,
            previousWeekStart,
            allocationThrough,
            reminderCount,
            cancellationToken);

        preparedEmployees.Add(
            $"{profile.User.Username} ({profile.User.Email}) — ReminderCount={reminderCount}");
        return true;
    }

    private async Task<bool> PrepareFreezeScenarioForPreferredAsync(
        List<string> preparedEmployees,
        IReadOnlyList<string> preferredUsernames,
        IReadOnlyList<ResourceProfile> employeeProfiles,
        DateOnly previousWeekStart,
        DateOnly allocationThrough,
        CancellationToken cancellationToken)
    {
        var profile = await ResolveProfileAsync(preferredUsernames, employeeProfiles, cancellationToken);
        if (profile is null)
        {
            _logger.LogWarning(
                "No employee profile found for freeze scenario. Preferred: {Usernames}",
                string.Join(", ", preferredUsernames));
            return false;
        }

        await PrepareTimesheetFreezeForProfileAsync(
            profile,
            previousWeekStart,
            allocationThrough,
            cancellationToken);

        preparedEmployees.Add($"{profile.User.Username} ({profile.User.Email}) — Freeze (ReminderCount=2)");
        return true;
    }

    private async Task<ResourceProfile?> ResolveProfileAsync(
        IReadOnlyList<string> preferredUsernames,
        IReadOnlyList<ResourceProfile> employeeProfiles,
        CancellationToken cancellationToken)
    {
        foreach (var username in preferredUsernames)
        {
            var preferred = employeeProfiles.FirstOrDefault(profile =>
                    profile.User.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                ?? await LoadProfileAsync(username, cancellationToken);

            if (preferred is not null && _usedProfileIds.Add(preferred.Id))
            {
                return preferred;
            }
        }

        var next = employeeProfiles.FirstOrDefault(profile => _usedProfileIds.Add(profile.Id));
        return next;
    }

    private async Task<List<ResourceProfile>> GetActiveEmployeeProfilesAsync(CancellationToken cancellationToken) =>
        await _context.ResourceProfiles
            .Include(r => r.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(r => r.Allocations)
            .Where(r => r.User.IsActive)
            .Where(r => r.User.UserRoles.Any(assignment => assignment.Role.RoleName == EmployeeRoleName))
            .OrderBy(r => r.User.Username)
            .ToListAsync(cancellationToken);

    private async Task PrepareTimesheetReminderForProfileAsync(
        ResourceProfile profile,
        DateOnly previousWeekStart,
        DateOnly allocationThrough,
        int reminderCount,
        CancellationToken cancellationToken)
    {
        profile.TimesheetSubmissionFrozen = false;
        profile.TimesheetFrozenAt = null;

        await EnsureActiveAllocationAsync(profile, allocationThrough, cancellationToken);
        await RemoveTimesheetForWeekAsync(profile.Id, previousWeekStart, cancellationToken);
        await RemoveComplianceForWeekAsync(profile.Id, previousWeekStart, cancellationToken);
        await ClearTimesheetNotificationLogsAsync(profile.Id, previousWeekStart, cancellationToken);

        if (reminderCount <= 0)
        {
            return;
        }

        var compliance = new TimesheetCompliance
        {
            ResourceProfileId = profile.Id,
            WeekStart = previousWeekStart,
            Status = TimesheetComplianceStatus.Pending,
            ReminderCount = reminderCount,
            Reminder1SentAt = DateTime.UtcNow.AddDays(-1)
        };

        if (reminderCount >= 2)
        {
            compliance.Reminder2SentAt = DateTime.UtcNow.AddHours(-1);
        }

        _context.TimesheetCompliances.Add(compliance);
    }

    private async Task PrepareTimesheetFreezeForProfileAsync(
        ResourceProfile profile,
        DateOnly previousWeekStart,
        DateOnly allocationThrough,
        CancellationToken cancellationToken)
    {
        profile.TimesheetSubmissionFrozen = false;
        profile.TimesheetFrozenAt = null;

        await EnsureActiveAllocationAsync(profile, allocationThrough, cancellationToken);
        await RemoveTimesheetForWeekAsync(profile.Id, previousWeekStart, cancellationToken);
        await RemoveComplianceForWeekAsync(profile.Id, previousWeekStart, cancellationToken);
        await ClearTimesheetNotificationLogsAsync(profile.Id, previousWeekStart, cancellationToken);

        _context.TimesheetCompliances.Add(new TimesheetCompliance
        {
            ResourceProfileId = profile.Id,
            WeekStart = previousWeekStart,
            Status = TimesheetComplianceStatus.Pending,
            ReminderCount = 2,
            Reminder1SentAt = DateTime.UtcNow.AddDays(-2),
            Reminder2SentAt = DateTime.UtcNow.AddDays(-1)
        });
    }

    private async Task PrepareFrozenRestoreDemoUserAsync(
        DateOnly previousWeekStart,
        DateOnly allocationThrough,
        CancellationToken cancellationToken)
    {
        const string username = "frozen.test";
        var manager = await _context.Users.FirstOrDefaultAsync(u => u.Username == "ankit.shah", cancellationToken);
        var betaProject = await _context.Projects.FirstOrDefaultAsync(p => p.Name == "Beta CRM", cancellationToken);
        if (manager is null || betaProject is null)
        {
            return;
        }

        var user = await _context.Users
            .Include(u => u.ResourceProfile)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Username = username,
                Email = "frozen.test@techserve.com",
                FullName = "Frozen Test User",
                Department = "QA",
                Designation = "QAEngineer",
                IsActive = true,
                JoinedAt = todayFromUtc(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@1234")
            };

            var employeeRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == EmployeeRoleName, cancellationToken);

            if (employeeRole is not null)
            {
                user.UserRoles.Add(new UserRoleAssignment { Role = employeeRole, IsPrimary = true });
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            var profile = new ResourceProfile
            {
                UserId = user.Id,
                ManagerUserId = manager.Id,
                ResourceStatus = ResourceStatus.Allocated,
                TimesheetSubmissionFrozen = true,
                TimesheetFrozenAt = DateTime.UtcNow
            };

            _context.ResourceProfiles.Add(profile);
            await _context.SaveChangesAsync(cancellationToken);

            _context.Allocations.Add(new Allocation
            {
                ResourceProfileId = profile.Id,
                ProjectId = betaProject.Id,
                UtilisationPercent = 50,
                FromDate = previousWeekStart.AddMonths(-1),
                ToDate = allocationThrough
            });

            await ClearTimesheetNotificationLogsAsync(profile.Id, previousWeekStart, cancellationToken);
            _context.TimesheetCompliances.Add(new TimesheetCompliance
            {
                ResourceProfileId = profile.Id,
                WeekStart = previousWeekStart,
                Status = TimesheetComplianceStatus.Missed,
                ReminderCount = 2,
                Reminder1SentAt = DateTime.UtcNow.AddDays(-3),
                Reminder2SentAt = DateTime.UtcNow.AddDays(-2),
                FreezeNotifiedAt = DateTime.UtcNow.AddDays(-1)
            });

            return;
        }

        var resourceProfile = user.ResourceProfile
            ?? await _context.ResourceProfiles.FirstOrDefaultAsync(r => r.UserId == user.Id, cancellationToken);

        if (resourceProfile is null)
        {
            resourceProfile = new ResourceProfile
            {
                UserId = user.Id,
                ManagerUserId = manager.Id,
                ResourceStatus = ResourceStatus.Allocated
            };
            _context.ResourceProfiles.Add(resourceProfile);
        }

        resourceProfile.ManagerUserId = manager.Id;
        resourceProfile.TimesheetSubmissionFrozen = true;
        resourceProfile.TimesheetFrozenAt = DateTime.UtcNow;

        await EnsureActiveAllocationAsync(resourceProfile, allocationThrough, cancellationToken);
        await RemoveTimesheetForWeekAsync(resourceProfile.Id, previousWeekStart, cancellationToken);
        await RemoveComplianceForWeekAsync(resourceProfile.Id, previousWeekStart, cancellationToken);
        await ClearTimesheetNotificationLogsAsync(resourceProfile.Id, previousWeekStart, cancellationToken);

        _context.TimesheetCompliances.Add(new TimesheetCompliance
        {
            ResourceProfileId = resourceProfile.Id,
            WeekStart = previousWeekStart,
            Status = TimesheetComplianceStatus.Missed,
            ReminderCount = 2,
            Reminder1SentAt = DateTime.UtcNow.AddDays(-3),
            Reminder2SentAt = DateTime.UtcNow.AddDays(-2),
            FreezeNotifiedAt = DateTime.UtcNow.AddDays(-1)
        });
    }

    private async Task PrepareAtRiskEmailScenarioAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var betaProject = await _context.Projects
            .Include(p => p.Milestones)
            .FirstOrDefaultAsync(p => p.Name == "Beta CRM", cancellationToken);

        if (betaProject is null)
        {
            _logger.LogWarning("Beta CRM project not found — skipping at-risk notification test data.");
            return;
        }

        betaProject.HealthStatus = HealthStatus.OnTrack;
        betaProject.Status = ProjectStatus.Active;

        var overdueMilestone = betaProject.Milestones
            .FirstOrDefault(milestone => milestone.Title == AtRiskMilestoneTitle);

        if (overdueMilestone is null)
        {
            betaProject.Milestones.Add(new Milestone
            {
                Title = AtRiskMilestoneTitle,
                DueDate = today.AddDays(-5),
                StoryPoints = 10,
                Status = MilestoneStatus.NotStarted
            });
        }
        else
        {
            overdueMilestone.DueDate = today.AddDays(-5);
            overdueMilestone.Status = MilestoneStatus.NotStarted;
        }

        var referencePrefix = $"project:{betaProject.Id}:atrisk:";
        var atRiskLogs = await _context.NotificationLogs
            .Where(log =>
                log.NotificationType == NotificationType.ProjectAtRisk
                && log.ReferenceKey.StartsWith(referencePrefix))
            .ToListAsync(cancellationToken);

        _context.NotificationLogs.RemoveRange(atRiskLogs);
    }

    private async Task ClearAllTimesheetNotificationLogsAsync(CancellationToken cancellationToken)
    {
        var timesheetTypes = new[]
        {
            NotificationType.TimesheetReminder1,
            NotificationType.TimesheetReminder2,
            NotificationType.TimesheetFrozenEmployee,
            NotificationType.TimesheetFrozenManager
        };

        var logs = await _context.NotificationLogs
            .Where(log => timesheetTypes.Contains(log.NotificationType))
            .ToListAsync(cancellationToken);

        _context.NotificationLogs.RemoveRange(logs);

        _logger.LogInformation("Cleared {Count} timesheet notification log entries.", logs.Count);
    }

    private async Task<ResourceProfile?> LoadProfileAsync(string username, CancellationToken cancellationToken) =>
        await _context.ResourceProfiles
            .Include(r => r.User)
            .Include(r => r.Allocations)
            .FirstOrDefaultAsync(r => r.User.Username == username, cancellationToken);

    private async Task EnsureActiveAllocationAsync(
        ResourceProfile profile,
        DateOnly allocationThrough,
        CancellationToken cancellationToken)
    {
        var today = ActiveDateHelper.TodayUtc;
        var previousWeekStart = ActiveDateHelper.GetPreviousCompletedWeekStart();

        foreach (var allocation in profile.Allocations.Where(allocation => allocation.ToDate < allocationThrough))
        {
            allocation.ToDate = allocationThrough;
        }

        var hasActiveAllocation = profile.Allocations.Any(allocation => ActiveDateHelper.IsAllocationActive(allocation, today));
        var coversPreviousWeek = profile.Allocations.Any(allocation =>
            ActiveDateHelper.IsAllocationActiveDuringWeek(allocation, previousWeekStart));

        if (hasActiveAllocation && coversPreviousWeek)
        {
            return;
        }

        var project = await _context.Projects
            .Where(p => p.Status == ProjectStatus.Active)
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            _logger.LogWarning("No active project found to allocate {Username}", profile.User.Username);
            return;
        }

        var fromDate = previousWeekStart.AddMonths(-2);
        var allocations = await _context.Allocations
            .Where(allocation => allocation.ResourceProfileId == profile.Id)
            .ToListAsync(cancellationToken);

        var peakUtilisation = ActiveDateHelper.GetCriticalDatesInRange(fromDate, allocationThrough, allocations)
            .Select(date => ActiveDateHelper.SumUtilisationOnDate(allocations, date))
            .DefaultIfEmpty(0)
            .Max();

        if (peakUtilisation >= ValidationConstants.MaxUtilisationPercent)
        {
            _logger.LogWarning(
                "Skipped test allocation for {Username}. Existing peak utilisation is {Utilisation}%.",
                profile.User.Username,
                peakUtilisation);
            return;
        }

        var requestedUtilisation = Math.Min(50, ValidationConstants.MaxUtilisationPercent - peakUtilisation);
        if (requestedUtilisation < ValidationConstants.MinUtilisationPercent)
        {
            return;
        }

        _context.Allocations.Add(new Allocation
        {
            ResourceProfileId = profile.Id,
            ProjectId = project.Id,
            UtilisationPercent = requestedUtilisation,
            FromDate = fromDate,
            ToDate = allocationThrough
        });
    }

    private async Task RemoveTimesheetForWeekAsync(
        int resourceProfileId,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var timesheet = await _context.Timesheets
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(
                t => t.ResourceProfileId == resourceProfileId && t.WeekStart == weekStart,
                cancellationToken);

        if (timesheet is null)
        {
            return;
        }

        _context.TimesheetEntries.RemoveRange(timesheet.Entries);
        _context.Timesheets.Remove(timesheet);
    }

    private async Task RemoveComplianceForWeekAsync(
        int resourceProfileId,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var compliance = await _context.TimesheetCompliances
            .FirstOrDefaultAsync(
                c => c.ResourceProfileId == resourceProfileId && c.WeekStart == weekStart,
                cancellationToken);

        if (compliance is not null)
        {
            _context.TimesheetCompliances.Remove(compliance);
        }
    }

    private async Task ClearTimesheetNotificationLogsAsync(
        int resourceProfileId,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var prefix = $"compliance:{resourceProfileId}:{weekStart:yyyy-MM-dd}:";
        var logs = await _context.NotificationLogs
            .Where(log => log.ReferenceKey.StartsWith(prefix))
            .ToListAsync(cancellationToken);

        _context.NotificationLogs.RemoveRange(logs);
    }

    private static DateOnly todayFromUtc() => DateOnly.FromDateTime(DateTime.UtcNow);
}
