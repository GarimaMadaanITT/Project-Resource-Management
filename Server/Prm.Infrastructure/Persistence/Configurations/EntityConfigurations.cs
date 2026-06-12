using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Prm.Domain.Entities;



namespace Prm.Infrastructure.Persistence.Configurations;



public class UserConfiguration : IEntityTypeConfiguration<User>

{

    public void Configure(EntityTypeBuilder<User> builder)

    {

        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Username).HasMaxLength(100).IsRequired();

        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();

        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();

        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();

        builder.HasIndex(x => x.Username).IsUnique();

        builder.HasIndex(x => x.Email).IsUnique();

        builder.HasOne(x => x.ResourceProfile).WithOne(x => x.User).HasForeignKey<ResourceProfile>(x => x.UserId);

    }

}



public class ResourceProfileConfiguration : IEntityTypeConfiguration<ResourceProfile>

{

    public void Configure(EntityTypeBuilder<ResourceProfile> builder)

    {

        builder.ToTable("resource_profiles");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne(x => x.Manager).WithMany(x => x.ManagedResourceProfiles).HasForeignKey(x => x.ManagerUserId).OnDelete(DeleteBehavior.Restrict);

    }

}



public class RoleConfiguration : IEntityTypeConfiguration<Role>

{

    public void Configure(EntityTypeBuilder<Role> builder)

    {

        builder.ToTable("roles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RoleName).HasMaxLength(50).IsRequired();

        builder.HasIndex(x => x.RoleName).IsUnique();

    }

}



public class PermissionConfiguration : IEntityTypeConfiguration<Permission>

{

    public void Configure(EntityTypeBuilder<Permission> builder)

    {

        builder.ToTable("permissions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Resource).HasMaxLength(100).IsRequired();

        builder.Property(x => x.Action).HasMaxLength(50).IsRequired();

        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => new { x.Resource, x.Action }).IsUnique();

    }

}



public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>

{

    public void Configure(EntityTypeBuilder<RolePermission> builder)

    {

        builder.ToTable("role_permissions");

        builder.HasKey(x => new { x.RoleId, x.PermissionId });

        builder.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId);

        builder.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId);

    }

}



public class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>

{

    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)

    {

        builder.ToTable("user_roles");

        builder.HasKey(x => new { x.UserId, x.RoleId });

        builder.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId);

        builder.HasOne(x => x.AssignedByUser).WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.UserId, x.IsPrimary });

    }

}



public class SkillConfiguration : IEntityTypeConfiguration<Skill>

{

    public void Configure(EntityTypeBuilder<Skill> builder)

    {

        builder.ToTable("skills");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();

        builder.Property(x => x.CustomCategoryLabel).HasMaxLength(100);

        builder.HasIndex(x => x.Name).IsUnique();

    }

}



public class UserSkillConfiguration : IEntityTypeConfiguration<UserSkill>

{

    public void Configure(EntityTypeBuilder<UserSkill> builder)

    {

        builder.ToTable("user_skills");

        builder.HasKey(x => new { x.UserId, x.SkillId });

        builder.HasOne(x => x.User).WithMany(x => x.Skills).HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.Skill).WithMany(x => x.UserSkills).HasForeignKey(x => x.SkillId);

    }

}



public class ProjectConfiguration : IEntityTypeConfiguration<Project>

{

    public void Configure(EntityTypeBuilder<Project> builder)

    {

        builder.ToTable("projects");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.HasOne(x => x.Manager).WithMany(x => x.ManagedProjects).HasForeignKey(x => x.ManagerUserId).OnDelete(DeleteBehavior.Restrict);

    }

}



public class MilestoneConfiguration : IEntityTypeConfiguration<Milestone>

{

    public void Configure(EntityTypeBuilder<Milestone> builder)

    {

        builder.ToTable("project_milestones");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();

        builder.HasOne(x => x.Project).WithMany(x => x.Milestones).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);

    }

}



public class AllocationConfiguration : IEntityTypeConfiguration<Allocation>

{

    public void Configure(EntityTypeBuilder<Allocation> builder)

    {

        builder.ToTable("project_allocations");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.ResourceProfile).WithMany(x => x.Allocations).HasForeignKey(x => x.ResourceProfileId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Project).WithMany(x => x.Allocations).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ResourceProfileId, x.ProjectId, x.FromDate });

    }

}



public class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>

{

    public void Configure(EntityTypeBuilder<Timesheet> builder)

    {

        builder.ToTable("timesheets");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.ResourceProfile).WithMany(x => x.Timesheets).HasForeignKey(x => x.ResourceProfileId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ResourceProfileId, x.WeekStart }).IsUnique();

    }

}



public class TimesheetEntryConfiguration : IEntityTypeConfiguration<TimesheetEntry>

{

    public void Configure(EntityTypeBuilder<TimesheetEntry> builder)

    {

        builder.ToTable("timesheet_line_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActivityTags).HasMaxLength(1000);

        builder.HasOne(x => x.Timesheet).WithMany(x => x.Entries).HasForeignKey(x => x.TimesheetId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Project).WithMany(x => x.TimesheetEntries).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);

    }

}



public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>

{

    public void Configure(EntityTypeBuilder<SystemSetting> builder)

    {

        builder.ToTable("system_settings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LlmApiKey).HasMaxLength(500);

    }

}



public class SystemConfigurationEntityConfiguration : IEntityTypeConfiguration<SystemConfiguration>

{

    public void Configure(EntityTypeBuilder<SystemConfiguration> builder)

    {

        builder.ToTable("system_configurations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConfigKey).HasMaxLength(100).IsRequired();

        builder.Property(x => x.ConfigValue).HasColumnType("text");

        builder.HasIndex(x => x.ConfigKey).IsUnique();

        builder.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull);

    }

}



public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>

{

    public void Configure(EntityTypeBuilder<AuditLog> builder)

    {

        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityName).HasMaxLength(100).IsRequired();

        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();

        builder.Property(x => x.OldValue).HasColumnType("text");

        builder.Property(x => x.NewValue).HasColumnType("text");

        builder.Property(x => x.PerformedByRole).HasMaxLength(50);

        builder.Property(x => x.Source).HasMaxLength(20).IsRequired();

        builder.HasIndex(x => x.CreatedAtUtc);

        builder.HasIndex(x => new { x.EntityName, x.EntityId });

        builder.HasIndex(x => x.Source);

    }

}



public class ActivityTagConfiguration : IEntityTypeConfiguration<ActivityTag>

{

    public void Configure(EntityTypeBuilder<ActivityTag> builder)

    {

        builder.ToTable("activity_tags");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TagCode).HasMaxLength(50).IsRequired();

        builder.Property(x => x.TagName).HasMaxLength(100).IsRequired();

        builder.Property(x => x.TagCategory).HasMaxLength(50).IsRequired();

        builder.HasIndex(x => x.TagCode).IsUnique();

    }

}



public class AiRequestLogConfiguration : IEntityTypeConfiguration<AiRequestLog>

{

    public void Configure(EntityTypeBuilder<AiRequestLog> builder)

    {

        builder.ToTable("ai_request_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RequestType).HasMaxLength(50).IsRequired();

        builder.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);

    }

}



public class SchedulerJobLogConfiguration : IEntityTypeConfiguration<SchedulerJobLog>

{

    public void Configure(EntityTypeBuilder<SchedulerJobLog> builder)

    {

        builder.ToTable("scheduler_job_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobName).HasMaxLength(100).IsRequired();

        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();

        builder.Property(x => x.ErrorMessage).HasColumnType("text");

    }

}

