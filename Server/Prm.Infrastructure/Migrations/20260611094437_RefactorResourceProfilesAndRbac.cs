using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorResourceProfilesAndRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_allocations_employees_EmployeeId",
                table: "allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_allocations_projects_ProjectId",
                table: "allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_milestones_projects_ProjectId",
                table: "milestones");

            migrationBuilder.DropForeignKey(
                name: "FK_timesheet_entries_projects_ProjectId",
                table: "timesheet_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_timesheet_entries_timesheets_TimesheetId",
                table: "timesheet_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_timesheets_employees_EmployeeId",
                table: "timesheets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_timesheet_entries",
                table: "timesheet_entries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_milestones",
                table: "milestones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_allocations",
                table: "allocations");

            migrationBuilder.RenameTable(
                name: "timesheet_entries",
                newName: "timesheet_line_items");

            migrationBuilder.RenameTable(
                name: "milestones",
                newName: "project_milestones");

            migrationBuilder.RenameTable(
                name: "allocations",
                newName: "project_allocations");

            migrationBuilder.RenameColumn(
                name: "ForcePasswordChange",
                table: "users",
                newName: "IsTemporaryPassword");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "timesheets",
                newName: "ResourceProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_timesheets_EmployeeId_WeekStart",
                table: "timesheets",
                newName: "IX_timesheets_ResourceProfileId_WeekStart");

            migrationBuilder.RenameIndex(
                name: "IX_timesheet_entries_TimesheetId",
                table: "timesheet_line_items",
                newName: "IX_timesheet_line_items_TimesheetId");

            migrationBuilder.RenameIndex(
                name: "IX_timesheet_entries_ProjectId",
                table: "timesheet_line_items",
                newName: "IX_timesheet_line_items_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_milestones_ProjectId",
                table: "project_milestones",
                newName: "IX_project_milestones_ProjectId");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "project_allocations",
                newName: "ResourceProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_allocations_ProjectId",
                table: "project_allocations",
                newName: "IX_project_allocations_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_allocations_EmployeeId_ProjectId_FromDate",
                table: "project_allocations",
                newName: "IX_project_allocations_ResourceProfileId_ProjectId_FromDate");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Designation",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "JoinedAt",
                table: "users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_timesheet_line_items",
                table: "timesheet_line_items",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_project_milestones",
                table: "project_milestones",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_project_allocations",
                table: "project_allocations",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "activity_tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TagCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TagName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TagCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_request_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: false),
                    RequestType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Prompt = table.Column<string>(type: "text", nullable: true),
                    ResponseSummary = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_request_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_request_logs_users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Resource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "resource_profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ManagerUserId = table.Column<int>(type: "integer", nullable: true),
                    ResourceStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_resource_profiles_users_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_resource_profiles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.Sql("""
                INSERT INTO roles ("RoleName", "CreatedAt")
                VALUES ('Admin', NOW()), ('Manager', NOW()), ('Employee', NOW());
                """);

            migrationBuilder.CreateTable(
                name: "scheduler_job_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduler_job_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "system_configurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfigKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfigValue = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_configurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_system_configurations_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_skills",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    Proficiency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_skills", x => new { x.UserId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_user_skills_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_skills_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    PermissionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedByUserId = table.Column<int>(type: "integer", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activity_tags_TagCode",
                table: "activity_tags",
                column: "TagCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_request_logs_RequestedByUserId",
                table: "ai_request_logs",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_Resource_Action",
                table: "permissions",
                columns: new[] { "Resource", "Action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource_profiles_ManagerUserId",
                table: "resource_profiles",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_resource_profiles_UserId",
                table: "resource_profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_PermissionId",
                table: "role_permissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_roles_RoleName",
                table: "roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_configurations_ConfigKey",
                table: "system_configurations",
                column: "ConfigKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_configurations_UpdatedByUserId",
                table: "system_configurations",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_AssignedByUserId",
                table: "user_roles",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_RoleId",
                table: "user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_UserId_IsPrimary",
                table: "user_roles",
                columns: new[] { "UserId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_user_skills_SkillId",
                table: "user_skills",
                column: "SkillId");

            migrationBuilder.Sql("""
                INSERT INTO resource_profiles ("Id", "UserId", "ManagerUserId", "ResourceStatus", "CreatedAt", "UpdatedAt")
                SELECT e."Id", e."UserId", mgr."UserId", e."Status", e."CreatedAt", e."UpdatedAt"
                FROM employees e
                INNER JOIN users u ON e."UserId" = u."Id"
                LEFT JOIN employees mgr ON e."ManagerId" = mgr."Id"
                WHERE u."Role" = 'Employee'
                   OR EXISTS (SELECT 1 FROM project_allocations pa WHERE pa."ResourceProfileId" = e."Id")
                   OR EXISTS (SELECT 1 FROM timesheets t WHERE t."ResourceProfileId" = e."Id");

                SELECT setval(
                    pg_get_serial_sequence('resource_profiles', 'Id'),
                    COALESCE((SELECT MAX("Id") FROM resource_profiles), 1),
                    true);
                """);

            migrationBuilder.Sql("""
                INSERT INTO user_roles ("UserId", "RoleId", "IsPrimary", "AssignedAt")
                SELECT u."Id", r."Id", true, NOW()
                FROM users u
                INNER JOIN roles r ON r."RoleName" = u."Role";
                """);

            migrationBuilder.Sql("""
                INSERT INTO user_skills ("UserId", "SkillId", "Proficiency", "CreatedAt")
                SELECT e."UserId", es."SkillId", es."Proficiency", NOW()
                FROM employee_skills es
                INNER JOIN employees e ON es."EmployeeId" = e."Id";
                """);

            migrationBuilder.Sql("""
                UPDATE users u
                SET "Department" = CASE
                    WHEN e."Department" IN ('Delivery', 'Engineering', 'Backend', 'Frontend', 'QA', 'DevOps', 'IT', 'HR')
                        THEN e."Department"
                    WHEN e."Department" IN ('Automation Testing', 'Testing') THEN 'QA'
                    ELSE 'Engineering'
                END
                FROM employees e
                WHERE e."UserId" = u."Id" AND u."Department" IS NULL;
                """);

            migrationBuilder.DropTable(
                name: "employee_skills");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "users");

            migrationBuilder.AddForeignKey(
                name: "FK_project_allocations_projects_ProjectId",
                table: "project_allocations",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_project_allocations_resource_profiles_ResourceProfileId",
                table: "project_allocations",
                column: "ResourceProfileId",
                principalTable: "resource_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_project_milestones_projects_ProjectId",
                table: "project_milestones",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_timesheet_line_items_projects_ProjectId",
                table: "timesheet_line_items",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_timesheet_line_items_timesheets_TimesheetId",
                table: "timesheet_line_items",
                column: "TimesheetId",
                principalTable: "timesheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_timesheets_resource_profiles_ResourceProfileId",
                table: "timesheets",
                column: "ResourceProfileId",
                principalTable: "resource_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project_allocations_projects_ProjectId",
                table: "project_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_project_allocations_resource_profiles_ResourceProfileId",
                table: "project_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_project_milestones_projects_ProjectId",
                table: "project_milestones");

            migrationBuilder.DropForeignKey(
                name: "FK_timesheet_line_items_projects_ProjectId",
                table: "timesheet_line_items");

            migrationBuilder.DropForeignKey(
                name: "FK_timesheet_line_items_timesheets_TimesheetId",
                table: "timesheet_line_items");

            migrationBuilder.DropForeignKey(
                name: "FK_timesheets_resource_profiles_ResourceProfileId",
                table: "timesheets");

            migrationBuilder.DropTable(
                name: "activity_tags");

            migrationBuilder.DropTable(
                name: "ai_request_logs");

            migrationBuilder.DropTable(
                name: "resource_profiles");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "scheduler_job_logs");

            migrationBuilder.DropTable(
                name: "system_configurations");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "user_skills");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_timesheet_line_items",
                table: "timesheet_line_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_project_milestones",
                table: "project_milestones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_project_allocations",
                table: "project_allocations");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Designation",
                table: "users");

            migrationBuilder.DropColumn(
                name: "JoinedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "users");

            migrationBuilder.RenameTable(
                name: "timesheet_line_items",
                newName: "timesheet_entries");

            migrationBuilder.RenameTable(
                name: "project_milestones",
                newName: "milestones");

            migrationBuilder.RenameTable(
                name: "project_allocations",
                newName: "allocations");

            migrationBuilder.RenameColumn(
                name: "IsTemporaryPassword",
                table: "users",
                newName: "ForcePasswordChange");

            migrationBuilder.RenameColumn(
                name: "ResourceProfileId",
                table: "timesheets",
                newName: "EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_timesheets_ResourceProfileId_WeekStart",
                table: "timesheets",
                newName: "IX_timesheets_EmployeeId_WeekStart");

            migrationBuilder.RenameIndex(
                name: "IX_timesheet_line_items_TimesheetId",
                table: "timesheet_entries",
                newName: "IX_timesheet_entries_TimesheetId");

            migrationBuilder.RenameIndex(
                name: "IX_timesheet_line_items_ProjectId",
                table: "timesheet_entries",
                newName: "IX_timesheet_entries_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_project_milestones_ProjectId",
                table: "milestones",
                newName: "IX_milestones_ProjectId");

            migrationBuilder.RenameColumn(
                name: "ResourceProfileId",
                table: "allocations",
                newName: "EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_project_allocations_ResourceProfileId_ProjectId_FromDate",
                table: "allocations",
                newName: "IX_allocations_EmployeeId_ProjectId_FromDate");

            migrationBuilder.RenameIndex(
                name: "IX_project_allocations_ProjectId",
                table: "allocations",
                newName: "IX_allocations_ProjectId");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_timesheet_entries",
                table: "timesheet_entries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_milestones",
                table: "milestones",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_allocations",
                table: "allocations",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ManagerId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employees_employees_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employees_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_skills",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    Proficiency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_skills", x => new { x.EmployeeId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_employee_skills_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_skills_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_skills_SkillId",
                table: "employee_skills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_ManagerId",
                table: "employees",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_UserId",
                table: "employees",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_allocations_employees_EmployeeId",
                table: "allocations",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_allocations_projects_ProjectId",
                table: "allocations",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestones_projects_ProjectId",
                table: "milestones",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_timesheet_entries_projects_ProjectId",
                table: "timesheet_entries",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_timesheet_entries_timesheets_TimesheetId",
                table: "timesheet_entries",
                column: "TimesheetId",
                principalTable: "timesheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_timesheets_employees_EmployeeId",
                table: "timesheets",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
