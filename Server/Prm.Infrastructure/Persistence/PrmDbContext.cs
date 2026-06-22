using Microsoft.EntityFrameworkCore;

using Prm.Domain.Entities;

using Prm.Domain.Enums;



namespace Prm.Infrastructure.Persistence;



public class PrmDbContext : DbContext

{

    public PrmDbContext(DbContextOptions<PrmDbContext> options) : base(options)

    {

    }



    public DbSet<User> Users => Set<User>();

    public DbSet<ResourceProfile> ResourceProfiles => Set<ResourceProfile>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserRoleAssignment> UserRoles => Set<UserRoleAssignment>();

    public DbSet<Skill> Skills => Set<Skill>();

    public DbSet<UserSkill> UserSkills => Set<UserSkill>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Milestone> Milestones => Set<Milestone>();

    public DbSet<Allocation> Allocations => Set<Allocation>();

    public DbSet<Timesheet> Timesheets => Set<Timesheet>();

    public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<AiRequestLog> AiRequestLogs => Set<AiRequestLog>();

    public DbSet<SchedulerJobLog> SchedulerJobLogs => Set<SchedulerJobLog>();

    public DbSet<TimesheetCompliance> TimesheetCompliances => Set<TimesheetCompliance>();

    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)

    {

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrmDbContext).Assembly);

        ConfigureEnumConversions(modelBuilder);

    }



    private static void ConfigureEnumConversions(ModelBuilder modelBuilder)

    {

        modelBuilder.Entity<User>().Property(x => x.Department).HasConversion<string>().HasMaxLength(50);

        modelBuilder.Entity<User>().Property(x => x.Designation).HasConversion<string>().HasMaxLength(50);

        modelBuilder.Entity<ResourceProfile>().Property(x => x.ResourceStatus).HasConversion<string>().HasMaxLength(20);

        modelBuilder.Entity<Project>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        modelBuilder.Entity<Project>().Property(x => x.HealthStatus).HasConversion<string>().HasMaxLength(20);

        modelBuilder.Entity<Milestone>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        modelBuilder.Entity<Timesheet>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        modelBuilder.Entity<Skill>().Property(x => x.Category).HasConversion<string>().HasMaxLength(20);

        modelBuilder.Entity<UserSkill>().Property(x => x.Proficiency).HasConversion<string>().HasMaxLength(20);

        modelBuilder.Entity<SystemSetting>().Property(x => x.LlmProvider).HasConversion<string>().HasMaxLength(20);

    }

}

