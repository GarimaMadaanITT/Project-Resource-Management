using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

using Prm.Application.DTOs.Admin;

namespace Prm.Infrastructure.Persistence.Seeding;

public class DataSeeder
{
    private readonly PrmDbContext _context;
    private readonly ILogger<DataSeeder> _logger;
    private readonly NotificationTestDataSeeder _notificationTestDataSeeder;

    public DataSeeder(
        PrmDbContext context,
        ILogger<DataSeeder> logger,
        NotificationTestDataSeeder notificationTestDataSeeder)
    {
        _context = context;
        _logger = logger;
        _notificationTestDataSeeder = notificationTestDataSeeder;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Users.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already seeded. Skipping seed data.");
            return;
        }

        _logger.LogInformation("Seeding database...");

        var roles = await SeedRolesAsync(cancellationToken);
        await SeedSystemSettingsAsync(cancellationToken);

        var skills = await SeedSkillsAsync(cancellationToken);

        var admin = CreateUser(
            "admin",
            "admin@techserve.com",
            "System Admin",
            "IT",
            "SystemAdministrator",
            "Admin@1234",
            isTemporaryPassword: true);
        _context.Users.Add(admin);

        var ankitUser = CreateUser("ankit.shah", "ankit.shah@techserve.com", "Ankit Shah", "Delivery", "ProjectManager", "Manager@1234");
        var nehaManagerUser = CreateUser("neha.joshi", "neha.joshi@techserve.com", "Neha Joshi", "Delivery", "SeniorProjectManager", "Manager@1234");
        var rohanUser = CreateUser("rohan.verma", "rohan.verma@techserve.com", "Rohan Verma", "Delivery", "ProjectManager", "Manager@1234");

        var raviUser = CreateUser("ravi.kumar", "ravi.kumar@techserve.com", "Ravi Kumar", "Backend", "SoftwareEngineer", "Employee@1234");
        var priyaUser = CreateUser("priya.sharma", "priya.sharma@techserve.com", "Priya Sharma", "Frontend", "SoftwareEngineer", "Employee@1234", isActive: false);
        var anilUser = CreateUser("anil.mehta", "anil.mehta@techserve.com", "Anil Mehta", "DevOps", "DevOpsEngineer", "Employee@1234");
        var saraUser = CreateUser("sara.khan", "sara.khan@techserve.com", "Sara Khan", "QA", "QAEngineer", "Employee@1234");
        var devUser = CreateUser("dev.patel", "dev.patel@techserve.com", "Dev Patel", "Backend", "SeniorSoftwareEngineer", "Employee@1234");

        _context.Users.AddRange(ankitUser, nehaManagerUser, rohanUser, raviUser, priyaUser, anilUser, saraUser, devUser);
        await _context.SaveChangesAsync(cancellationToken);

        AssignRole(admin, roles[UserRole.Admin]);
        AssignRole(ankitUser, roles[UserRole.Manager]);
        AssignRole(nehaManagerUser, roles[UserRole.Manager]);
        AssignRole(rohanUser, roles[UserRole.Manager]);
        AssignRole(raviUser, roles[UserRole.Employee]);
        AssignRole(priyaUser, roles[UserRole.Employee]);
        AssignRole(anilUser, roles[UserRole.Employee]);
        AssignRole(saraUser, roles[UserRole.Employee]);
        AssignRole(devUser, roles[UserRole.Employee]);
        await _context.SaveChangesAsync(cancellationToken);

        var ravi = new ResourceProfile { UserId = raviUser.Id, ResourceStatus = ResourceStatus.Allocated, ManagerUserId = ankitUser.Id };
        var priya = new ResourceProfile { UserId = priyaUser.Id, ResourceStatus = ResourceStatus.Bench, ManagerUserId = ankitUser.Id };
        var anil = new ResourceProfile { UserId = anilUser.Id, ResourceStatus = ResourceStatus.Bench, ManagerUserId = ankitUser.Id };
        var sara = new ResourceProfile { UserId = saraUser.Id, ResourceStatus = ResourceStatus.Allocated, ManagerUserId = ankitUser.Id };
        var dev = new ResourceProfile { UserId = devUser.Id, ResourceStatus = ResourceStatus.Allocated, ManagerUserId = ankitUser.Id };

        _context.ResourceProfiles.AddRange(ravi, priya, anil, sara, dev);
        await _context.SaveChangesAsync(cancellationToken);

        AddUserSkills(raviUser, skills, ("Java", ProficiencyLevel.Intermediate), ("Spring Boot", ProficiencyLevel.Advanced), ("MySQL", ProficiencyLevel.Intermediate));
        AddUserSkills(priyaUser, skills, ("React", ProficiencyLevel.Advanced), ("TypeScript", ProficiencyLevel.Advanced), ("CSS", ProficiencyLevel.Intermediate));
        AddUserSkills(anilUser, skills, ("Docker", ProficiencyLevel.Advanced), ("Kubernetes", ProficiencyLevel.Intermediate), ("CI/CD", ProficiencyLevel.Advanced));
        AddUserSkills(saraUser, skills, ("Python", ProficiencyLevel.Intermediate), ("Django", ProficiencyLevel.Intermediate), ("PostgreSQL", ProficiencyLevel.Beginner));
        AddUserSkills(devUser, skills, ("Java", ProficiencyLevel.Advanced), ("Spring Boot", ProficiencyLevel.Intermediate), ("React", ProficiencyLevel.Beginner));

        var alphaPortal = new Project
        {
            Name = "Alpha Portal",
            Description = "Customer web portal",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            Status = ProjectStatus.Active,
            ManagerUserId = ankitUser.Id,
            TotalStoryPoints = 120,
            HealthStatus = HealthStatus.AtRisk
        };

        var betaCrm = new Project
        {
            Name = "Beta CRM",
            Description = "CRM modernization",
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2026, 8, 15),
            Status = ProjectStatus.Active,
            ManagerUserId = ankitUser.Id,
            TotalStoryPoints = 80,
            HealthStatus = HealthStatus.OnTrack
        };

        var gammaRewrite = new Project
        {
            Name = "Gamma Rewrite",
            Description = "Legacy rewrite program",
            StartDate = new DateOnly(2026, 1, 15),
            EndDate = new DateOnly(2026, 7, 1),
            Status = ProjectStatus.Active,
            ManagerUserId = nehaManagerUser.Id,
            TotalStoryPoints = 60,
            HealthStatus = HealthStatus.Attention
        };

        var deltaMigrate = new Project
        {
            Name = "Delta Migrate",
            Description = "Cloud migration",
            StartDate = new DateOnly(2026, 4, 1),
            EndDate = new DateOnly(2026, 9, 30),
            Status = ProjectStatus.Planned,
            ManagerUserId = rohanUser.Id,
            TotalStoryPoints = 100,
            HealthStatus = HealthStatus.OnTrack
        };

        _context.Projects.AddRange(alphaPortal, betaCrm, gammaRewrite, deltaMigrate);
        await _context.SaveChangesAsync(cancellationToken);

        _context.Milestones.AddRange(
            new Milestone { ProjectId = alphaPortal.Id, Title = "Design Complete", DueDate = new DateOnly(2026, 4, 1), StoryPoints = 20, Status = MilestoneStatus.Done },
            new Milestone { ProjectId = alphaPortal.Id, Title = "Backend API", DueDate = new DateOnly(2026, 4, 15), StoryPoints = 40, Status = MilestoneStatus.InProgress },
            new Milestone { ProjectId = alphaPortal.Id, Title = "Testing", DueDate = new DateOnly(2026, 4, 30), StoryPoints = 35, Status = MilestoneStatus.NotStarted },
            new Milestone { ProjectId = alphaPortal.Id, Title = "Go Live", DueDate = new DateOnly(2026, 5, 15), StoryPoints = 25, Status = MilestoneStatus.NotStarted });

        _context.Allocations.AddRange(
            new Allocation { ResourceProfileId = ravi.Id, ProjectId = alphaPortal.Id, UtilisationPercent = 50, FromDate = new DateOnly(2026, 3, 1), ToDate = new DateOnly(2026, 6, 30) },
            new Allocation { ResourceProfileId = ravi.Id, ProjectId = betaCrm.Id, UtilisationPercent = 50, FromDate = new DateOnly(2026, 4, 1), ToDate = new DateOnly(2026, 7, 31) },
            new Allocation { ResourceProfileId = dev.Id, ProjectId = betaCrm.Id, UtilisationPercent = 50, FromDate = new DateOnly(2026, 3, 15), ToDate = new DateOnly(2026, 8, 15) },
            new Allocation { ResourceProfileId = sara.Id, ProjectId = gammaRewrite.Id, UtilisationPercent = 75, FromDate = new DateOnly(2026, 2, 1), ToDate = new DateOnly(2026, 7, 1) });

        await _context.SaveChangesAsync(cancellationToken);

        var weekStart = new DateOnly(2026, 5, 4);
        var raviTimesheet = new Timesheet
        {
            ResourceProfileId = ravi.Id,
            WeekStart = weekStart,
            Status = TimesheetStatus.Submitted,
            TotalHours = 40,
            Entries =
            {
                new TimesheetEntry { ProjectId = alphaPortal.Id, Hours = 20, ActivityTags = "Microservices, WebSocket" },
                new TimesheetEntry { ProjectId = betaCrm.Id, Hours = 20, ActivityTags = "Backend API, Bug Fixing" }
            }
        };

        _context.Timesheets.Add(raviTimesheet);
        await _context.SaveChangesAsync(cancellationToken);

        await _notificationTestDataSeeder.SeedAsync(cancellationToken);

        _logger.LogInformation("Database seed completed.");
    }

    public Task<NotificationTestDataSeedResponse> SeedNotificationTestDataAsync(CancellationToken cancellationToken = default) =>
        _notificationTestDataSeeder.SeedAsync(cancellationToken);

    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Clearing all data from the database...");

        if (_context.Database.IsNpgsql())
        {
            await _context.Database.ExecuteSqlRawAsync(
                """
                TRUNCATE TABLE audit_logs, notification_logs, timesheet_compliance, timesheet_line_items, timesheets, project_allocations, user_skills, project_milestones, resource_profiles, projects, skills, user_roles, role_permissions, permissions, roles, users, system_settings, ai_request_logs, scheduler_job_logs
                RESTART IDENTITY CASCADE;
                """,
                cancellationToken);
        }
        else
        {
            await _context.TimesheetEntries.ExecuteDeleteAsync(cancellationToken);
            await _context.Timesheets.ExecuteDeleteAsync(cancellationToken);
            await _context.NotificationLogs.ExecuteDeleteAsync(cancellationToken);
            await _context.TimesheetCompliances.ExecuteDeleteAsync(cancellationToken);
            await _context.AuditLogs.ExecuteDeleteAsync(cancellationToken);
            await _context.Allocations.ExecuteDeleteAsync(cancellationToken);
            await _context.UserSkills.ExecuteDeleteAsync(cancellationToken);
            await _context.Milestones.ExecuteDeleteAsync(cancellationToken);
            await _context.ResourceProfiles.ExecuteUpdateAsync(
                r => r.SetProperty(x => x.ManagerUserId, (int?)null),
                cancellationToken);
            await _context.ResourceProfiles.ExecuteDeleteAsync(cancellationToken);
            await _context.Projects.ExecuteDeleteAsync(cancellationToken);
            await _context.Skills.ExecuteDeleteAsync(cancellationToken);
            await _context.UserRoles.ExecuteDeleteAsync(cancellationToken);
            await _context.RolePermissions.ExecuteDeleteAsync(cancellationToken);
            await _context.Permissions.ExecuteDeleteAsync(cancellationToken);
            await _context.Roles.ExecuteDeleteAsync(cancellationToken);
            await _context.Users.ExecuteDeleteAsync(cancellationToken);
            await _context.SystemSettings.ExecuteDeleteAsync(cancellationToken);
        }

        _logger.LogInformation("All database data cleared.");
    }

    public async Task ClearAndReseedAsync(CancellationToken cancellationToken = default)
    {
        await ClearAllDataAsync(cancellationToken);
        await SeedAsync(cancellationToken);
    }

    private async Task<Dictionary<UserRole, Role>> SeedRolesAsync(CancellationToken cancellationToken)
    {
        var roleNames = new Dictionary<UserRole, string>
        {
            [UserRole.Admin] = AuthConstants.RoleName(UserRole.Admin),
            [UserRole.Manager] = AuthConstants.RoleName(UserRole.Manager),
            [UserRole.Employee] = AuthConstants.RoleName(UserRole.Employee)
        };

        foreach (var roleName in roleNames.Values)
        {
            if (!await _context.Roles.AnyAsync(r => r.RoleName == roleName, cancellationToken))
            {
                _context.Roles.Add(new Role { RoleName = roleName });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var roles = await _context.Roles.ToListAsync(cancellationToken);
        return roles.ToDictionary(r => AuthConstants.ParseRole(r.RoleName));
    }

    private async Task SeedSystemSettingsAsync(CancellationToken cancellationToken)
    {
        _context.SystemSettings.Add(new SystemSetting
        {
            Id = 1,
            LlmProvider = LlmProviderType.Gemini,
            LlmApiKey = string.Empty,
            SchedulerIntervalHours = 4,
            MaxWeeklyHours = 40
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, Skill>> SeedSkillsAsync(CancellationToken cancellationToken)
    {
        var skillDefinitions = new (string Name, SkillCategory Category)[]
        {
            ("Java", SkillCategory.Backend),
            ("Spring Boot", SkillCategory.Backend),
            ("MySQL", SkillCategory.Backend),
            ("React", SkillCategory.Frontend),
            ("TypeScript", SkillCategory.Frontend),
            ("CSS", SkillCategory.Frontend),
            ("Docker", SkillCategory.DevOps),
            ("Kubernetes", SkillCategory.DevOps),
            ("CI/CD", SkillCategory.DevOps),
            ("Python", SkillCategory.Backend),
            ("Django", SkillCategory.Backend),
            ("PostgreSQL", SkillCategory.Backend),
            ("Microservices", SkillCategory.Backend),
            ("WebSocket", SkillCategory.Backend)
        };

        foreach (var (name, category) in skillDefinitions)
        {
            _context.Skills.Add(new Skill { Name = name, Category = category });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await _context.Skills.ToDictionaryAsync(s => s.Name, cancellationToken);
    }

    private static User CreateUser(
        string username,
        string email,
        string fullName,
        string? department,
        string? designation,
        string password,
        bool isTemporaryPassword = false,
        bool isActive = true)
    {
        return new User
        {
            Username = username,
            Email = email,
            FullName = fullName,
            Department = department,
            Designation = designation,
            IsActive = isActive,
            IsTemporaryPassword = isTemporaryPassword,
            JoinedAt = DateOnly.FromDateTime(DateTime.UtcNow),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };
    }

    private static void AssignRole(User user, Role role)
    {
        user.UserRoles.Add(new UserRoleAssignment
        {
            Role = role,
            IsPrimary = true
        });
    }

    private static void AddUserSkills(User user, Dictionary<string, Skill> skills, params (string SkillName, ProficiencyLevel Level)[] assignments)
    {
        foreach (var (skillName, level) in assignments)
        {
            if (skills.TryGetValue(skillName, out var skill))
            {
                user.Skills.Add(new UserSkill
                {
                    SkillId = skill.Id,
                    Skill = skill,
                    Proficiency = level
                });
            }
        }
    }
}
