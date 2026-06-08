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
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<EmployeeSkill> EmployeeSkills => Set<EmployeeSkill>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrmDbContext).Assembly);
        ConfigureEnumConversions(modelBuilder);
    }

    private static void ConfigureEnumConversions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Employee>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Project>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Project>().Property(x => x.HealthStatus).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Milestone>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Timesheet>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Skill>().Property(x => x.Category).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<EmployeeSkill>().Property(x => x.Proficiency).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<SystemSetting>().Property(x => x.LlmProvider).HasConversion<string>().HasMaxLength(20);
    }
}
