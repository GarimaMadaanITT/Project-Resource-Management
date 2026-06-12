using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Infrastructure.Persistence.Seeding;

public class DataSeeder
{
    private readonly PrmDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(PrmDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Users.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already seeded. Skipping seed data.");
            return;
        }

        _logger.LogInformation("Seeding database...");

        await SeedSystemSettingsAsync(cancellationToken);

        var skills = await SeedSkillsAsync(cancellationToken);

        var admin = CreateUser("admin", "admin@techserve.com", "System Admin", UserRole.Admin, "Admin@1234", forcePasswordChange: true);
        _context.Users.Add(admin);

        var ankitUser = CreateUser("ankit.shah", "ankit.shah@techserve.com", "Ankit Shah", UserRole.Manager, "Manager@1234");
        var nehaManagerUser = CreateUser("neha.joshi", "neha.joshi@techserve.com", "Neha Joshi", UserRole.Manager, "Manager@1234");
        var rohanUser = CreateUser("rohan.verma", "rohan.verma@techserve.com", "Rohan Verma", UserRole.Manager, "Manager@1234");

        var raviUser = CreateUser("ravi.kumar", "ravi.kumar@techserve.com", "Ravi Kumar", UserRole.Employee, "Employee@1234");
        var priyaUser = CreateUser("priya.sharma", "priya.sharma@techserve.com", "Priya Sharma", UserRole.Employee, "Employee@1234", isActive: false);
        var anilUser = CreateUser("anil.mehta", "anil.mehta@techserve.com", "Anil Mehta", UserRole.Employee, "Employee@1234");
        var saraUser = CreateUser("sara.khan", "sara.khan@techserve.com", "Sara Khan", UserRole.Employee, "Employee@1234");
        var devUser = CreateUser("dev.patel", "dev.patel@techserve.com", "Dev Patel", UserRole.Employee, "Employee@1234");

        _context.Users.AddRange(ankitUser, nehaManagerUser, rohanUser, raviUser, priyaUser, anilUser, saraUser, devUser);
        await _context.SaveChangesAsync(cancellationToken);

        var ankitEmployee = new Employee { UserId = ankitUser.Id, Department = "Delivery", Status = EmployeeStatus.Allocated, IsActive = true };
        var nehaManagerEmployee = new Employee { UserId = nehaManagerUser.Id, Department = "Delivery", Status = EmployeeStatus.Allocated, IsActive = true };
        var rohanEmployee = new Employee { UserId = rohanUser.Id, Department = "Delivery", Status = EmployeeStatus.Bench, IsActive = true };

        _context.Employees.AddRange(ankitEmployee, nehaManagerEmployee, rohanEmployee);
        await _context.SaveChangesAsync(cancellationToken);

        var ravi = new Employee { UserId = raviUser.Id, Department = "Backend", Status = EmployeeStatus.Allocated, ManagerId = ankitEmployee.Id, IsActive = true };
        var priya = new Employee { UserId = priyaUser.Id, Department = "Frontend", Status = EmployeeStatus.Bench, ManagerId = ankitEmployee.Id, IsActive = false };
        var anil = new Employee { UserId = anilUser.Id, Department = "DevOps", Status = EmployeeStatus.Bench, ManagerId = ankitEmployee.Id, IsActive = true };
        var sara = new Employee { UserId = saraUser.Id, Department = "QA", Status = EmployeeStatus.Allocated, ManagerId = ankitEmployee.Id, IsActive = true };
        var dev = new Employee { UserId = devUser.Id, Department = "Backend", Status = EmployeeStatus.Allocated, ManagerId = ankitEmployee.Id, IsActive = true };

        _context.Employees.AddRange(ravi, priya, anil, sara, dev);
        await _context.SaveChangesAsync(cancellationToken);

        AddEmployeeSkills(ravi, skills, ("Java", ProficiencyLevel.Intermediate), ("Spring Boot", ProficiencyLevel.Advanced), ("MySQL", ProficiencyLevel.Intermediate));
        AddEmployeeSkills(priya, skills, ("React", ProficiencyLevel.Advanced), ("TypeScript", ProficiencyLevel.Advanced), ("CSS", ProficiencyLevel.Intermediate));
        AddEmployeeSkills(anil, skills, ("Docker", ProficiencyLevel.Advanced), ("Kubernetes", ProficiencyLevel.Intermediate), ("CI/CD", ProficiencyLevel.Advanced));
        AddEmployeeSkills(sara, skills, ("Python", ProficiencyLevel.Intermediate), ("Django", ProficiencyLevel.Intermediate), ("PostgreSQL", ProficiencyLevel.Beginner));
        AddEmployeeSkills(dev, skills, ("Java", ProficiencyLevel.Advanced), ("Spring Boot", ProficiencyLevel.Intermediate), ("React", ProficiencyLevel.Beginner));

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
            new Allocation { EmployeeId = ravi.Id, ProjectId = alphaPortal.Id, UtilisationPercent = 50, FromDate = new DateOnly(2026, 3, 1), ToDate = new DateOnly(2026, 6, 30) },
            new Allocation { EmployeeId = ravi.Id, ProjectId = betaCrm.Id, UtilisationPercent = 50, FromDate = new DateOnly(2026, 4, 1), ToDate = new DateOnly(2026, 7, 31) },
            new Allocation { EmployeeId = dev.Id, ProjectId = betaCrm.Id, UtilisationPercent = 50, FromDate = new DateOnly(2026, 3, 15), ToDate = new DateOnly(2026, 8, 15) },
            new Allocation { EmployeeId = sara.Id, ProjectId = gammaRewrite.Id, UtilisationPercent = 75, FromDate = new DateOnly(2026, 2, 1), ToDate = new DateOnly(2026, 7, 1) });

        await _context.SaveChangesAsync(cancellationToken);

        var weekStart = new DateOnly(2026, 5, 4);
        var raviTimesheet = new Timesheet
        {
            EmployeeId = ravi.Id,
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

        _logger.LogInformation("Database seed completed.");
    }

    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Clearing all data from the database...");

        if (_context.Database.IsNpgsql())
        {
            await _context.Database.ExecuteSqlRawAsync(
                """
                TRUNCATE TABLE audit_logs, timesheet_entries, timesheets, allocations, employee_skills, milestones, employees, projects, skills, users, system_settings
                RESTART IDENTITY CASCADE;
                """,
                cancellationToken);
        }
        else
        {
            await _context.TimesheetEntries.ExecuteDeleteAsync(cancellationToken);
            await _context.Timesheets.ExecuteDeleteAsync(cancellationToken);
            await _context.AuditLogs.ExecuteDeleteAsync(cancellationToken);
            await _context.Allocations.ExecuteDeleteAsync(cancellationToken);
            await _context.EmployeeSkills.ExecuteDeleteAsync(cancellationToken);
            await _context.Milestones.ExecuteDeleteAsync(cancellationToken);
            await _context.Employees.ExecuteUpdateAsync(
                e => e.SetProperty(x => x.ManagerId, (int?)null),
                cancellationToken);
            await _context.Employees.ExecuteDeleteAsync(cancellationToken);
            await _context.Projects.ExecuteDeleteAsync(cancellationToken);
            await _context.Skills.ExecuteDeleteAsync(cancellationToken);
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
        UserRole role,
        string password,
        bool forcePasswordChange = false,
        bool isActive = true)
    {
        return new User
        {
            Username = username,
            Email = email,
            FullName = fullName,
            Role = role,
            IsActive = isActive,
            ForcePasswordChange = forcePasswordChange,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };
    }

    private static void AddEmployeeSkills(Employee employee, Dictionary<string, Skill> skills, params (string SkillName, ProficiencyLevel Level)[] assignments)
    {
        foreach (var (skillName, level) in assignments)
        {
            if (skills.TryGetValue(skillName, out var skill))
            {
                employee.Skills.Add(new EmployeeSkill
                {
                    Employee = employee,
                    SkillId = skill.Id,
                    Proficiency = level
                });
            }
        }
    }
}
