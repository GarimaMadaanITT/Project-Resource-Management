using Microsoft.Extensions.DependencyInjection;
using Prm.Application.Interfaces;
using Prm.Application.Services;
using Prm.Application.Services.Admin;
using Prm.Application.Services.Admin.Employees;
using Prm.Application.Services.Admin.Users;
using Prm.Application.Services.Manager;
using Prm.Application.Services.Employees;
using Prm.Application.Services.Shared;
using Prm.Application.Services.Scheduler;

namespace Prm.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountDeactivationService, AccountDeactivationService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        services.AddScoped<AdminEmployeeQueryService>();
        services.AddScoped<AdminEmployeeCommandService>();
        services.AddScoped<AdminEmployeeSkillService>();
        services.AddScoped<IAdminEmployeeService, AdminEmployeeService>();

        services.AddScoped<AdminUserProvisioningService>();
        services.AddScoped<AdminUserCredentialService>();
        services.AddScoped<AdminUserLifecycleService>();
        services.AddScoped<IAdminUserService, AdminUserService>();

        services.AddScoped<IAdminProjectService, AdminProjectService>();
        services.AddScoped<IAdminAllocationService, AdminAllocationService>();
        services.AddScoped<IAdminSettingsService, AdminSettingsService>();
        services.AddScoped<IAdminAuditLogQueryService, AdminAuditLogQueryService>();

        services.AddScoped<IManagerContextService, ManagerContextService>();
        services.AddScoped<IManagerDashboardService, ManagerDashboardService>();
        services.AddScoped<IManagerAllocationService, ManagerAllocationService>();
        services.AddScoped<IManagerProjectService, ManagerProjectService>();
        services.AddScoped<IManagerTeamTimesheetService, ManagerTeamTimesheetService>();
        services.AddScoped<IManagerAiService, ManagerAiService>();

        services.AddScoped<IEmployeeContextService, EmployeeContextService>();
        services.AddScoped<IEmployeeTimesheetService, EmployeeTimesheetService>();
        services.AddScoped<IEmployeeAllocationService, EmployeeAllocationService>();
        services.AddScoped<IEmployeeReminderService, EmployeeReminderService>();

        services.AddScoped<IUtilisationRecomputeService, UtilisationRecomputeService>();
        services.AddScoped<IProjectHealthRecomputeService, ProjectHealthRecomputeService>();
        services.AddScoped<ITimesheetMissedDetectionService, TimesheetMissedDetectionService>();
        services.AddScoped<ISchedulerOrchestrator, SchedulerOrchestrator>();

        return services;
    }
}
