using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Admin;

public class AdminProjectService : IAdminProjectService
{
    private readonly IProjectRepository _projects;
    private readonly IUserRepository _users;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<AdminProjectService> _logger;

    public AdminProjectService(
        IProjectRepository projects,
        IUserRepository users,
        IAuditLogService auditLog,
        ILogger<AdminProjectService> logger)
    {
        _projects = projects;
        _users = users;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProjectListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _projects.GetAllAsync(cancellationToken);
        return projects.Select(MapProject).ToList();
    }

    public async Task<ProjectListItemDto> CreateAsync(
        CreateProjectRequest request,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        var name = ProjectValidator.ValidateName(request.Name);
        var description = ProjectValidator.NormalizeDescription(request.Description);
        ProjectValidator.ValidateDates(request.StartDate, request.EndDate);
        ProjectValidator.ValidateStoryPoints(request.TotalStoryPoints);

        var manager = EntityGuard.EnsureFound(
            await _users.GetByIdAsync(request.ManagerUserId, cancellationToken),
            ErrorMessages.ManagerUserNotFound);
        ProjectValidator.ValidateManager(manager);

        var project = new Project
        {
            Name = name,
            Description = description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = EnumGuard.Parse<ProjectStatus>(StringGuard.RequireNonEmpty(request.Status, "Status"), "Status"),
            ManagerUserId = request.ManagerUserId,
            TotalStoryPoints = request.TotalStoryPoints,
            HealthStatus = DomainDefaults.NewProjectHealth
        };

        await _projects.AddAsync(project, cancellationToken);
        var created = EntityGuard.EnsureFound(
            await _projects.GetByIdAsync(project.Id, cancellationToken),
            ErrorMessages.ProjectNotFound);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.Project,
            created.Id,
            AuditConstants.Actions.Created,
            null,
            AuditSnapshotBuilder.ProjectSnapshot(created),
            actingUserId,
            AuthConstants.RoleName(UserRole.Admin),
            AuditConstants.Sources.User,
            cancellationToken);

        _logger.LogInformation(
            "Project created. ProjectId={ProjectId}, Name={ProjectName}, ManagerUserId={ManagerUserId}",
            created.Id,
            created.Name,
            created.ManagerUserId);

        return MapProject(created);
    }

    public async Task<ProjectListItemDto> UpdateAsync(
        int id,
        UpdateProjectRequest request,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        var project = EntityGuard.EnsureFound(
            await _projects.GetByIdAsync(id, cancellationToken),
            ErrorMessages.ProjectNotFound);

        var oldSnapshot = AuditSnapshotBuilder.ProjectSnapshot(project);
        var oldStatus = project.Status.ToString();

        var name = ProjectValidator.ValidateName(request.Name);
        var description = ProjectValidator.NormalizeDescription(request.Description);
        ProjectValidator.ValidateDates(request.StartDate, request.EndDate);
        ProjectValidator.ValidateStoryPoints(request.TotalStoryPoints);

        var manager = EntityGuard.EnsureFound(
            await _users.GetByIdAsync(request.ManagerUserId, cancellationToken),
            ErrorMessages.ManagerUserNotFound);
        ProjectValidator.ValidateManager(manager);

        var milestoneSum = await _projects.GetMilestoneStoryPointsSumAsync(id, cancellationToken: cancellationToken);
        ProjectValidator.ValidateStoryPointBudget(milestoneSum, request.TotalStoryPoints);

        project.Name = name;
        project.Description = description;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.Status = EnumGuard.Parse<ProjectStatus>(StringGuard.RequireNonEmpty(request.Status, "Status"), "Status");
        project.ManagerUserId = request.ManagerUserId;
        project.TotalStoryPoints = request.TotalStoryPoints;
        project.UpdatedAt = DateTime.UtcNow;

        await _projects.UpdateAsync(project, cancellationToken);

        var action = oldStatus == project.Status.ToString()
            ? AuditConstants.Actions.Updated
            : AuditConstants.Actions.StatusChanged;

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.Project,
            project.Id,
            action,
            oldSnapshot,
            AuditSnapshotBuilder.ProjectSnapshot(project),
            actingUserId,
            AuthConstants.RoleName(UserRole.Admin),
            AuditConstants.Sources.User,
            cancellationToken);

        _logger.LogInformation(
            "Project updated. ProjectId={ProjectId}, Name={ProjectName}, ManagerUserId={ManagerUserId}",
            project.Id,
            project.Name,
            project.ManagerUserId);

        return MapProject(project);
    }

    public async Task<IReadOnlyList<MilestoneDto>> GetMilestonesAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var project = EntityGuard.EnsureFound(
            await _projects.GetByIdAsync(projectId, cancellationToken),
            ErrorMessages.ProjectNotFound);

        return project.Milestones.OrderBy(milestone => milestone.DueDate).Select(MapMilestone).ToList();
    }

    public async Task<MilestoneDto> AddMilestoneAsync(
        int projectId,
        AddMilestoneRequest request,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        var project = EntityGuard.EnsureFound(
            await _projects.GetByIdAsync(projectId, cancellationToken),
            ErrorMessages.ProjectNotFound);

        var title = MilestoneValidator.ValidateTitle(request.Title);
        MilestoneValidator.ValidateStoryPoints(request.StoryPoints);
        MilestoneValidator.ValidateDueDateWithinProject(request.DueDate, project);

        var existingSum = await _projects.GetMilestoneStoryPointsSumAsync(projectId, cancellationToken: cancellationToken);
        MilestoneValidator.ValidateStoryPointBudget(existingSum, request.StoryPoints, project.TotalStoryPoints);

        var milestone = new Milestone
        {
            ProjectId = projectId,
            Title = title,
            DueDate = request.DueDate,
            StoryPoints = request.StoryPoints,
            Status = DomainDefaults.NewMilestoneStatus
        };

        await _projects.AddMilestoneAsync(milestone, cancellationToken);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.Milestone,
            milestone.Id,
            AuditConstants.Actions.Created,
            null,
            AuditSnapshotBuilder.MilestoneSnapshot(milestone),
            actingUserId,
            AuthConstants.RoleName(UserRole.Admin),
            AuditConstants.Sources.User,
            cancellationToken);

        return MapMilestone(milestone);
    }

    public async Task<MilestoneDto> UpdateMilestoneStatusAsync(
        int projectId,
        int milestoneId,
        UpdateMilestoneStatusRequest request,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        var milestone = EntityGuard.EnsureFound(
            await _projects.GetMilestoneAsync(projectId, milestoneId, cancellationToken),
            ErrorMessages.MilestoneNotFound);

        var oldSnapshot = AuditSnapshotBuilder.MilestoneSnapshot(milestone);

        milestone.Status = EnumGuard.Parse<MilestoneStatus>(
            StringGuard.RequireNonEmpty(request.Status, "Status"),
            "Status");
        milestone.UpdatedAt = DateTime.UtcNow;
        await _projects.UpdateMilestoneAsync(milestone, cancellationToken);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.Milestone,
            milestone.Id,
            AuditConstants.Actions.StatusChanged,
            oldSnapshot,
            AuditSnapshotBuilder.MilestoneSnapshot(milestone),
            actingUserId,
            AuthConstants.RoleName(UserRole.Admin),
            AuditConstants.Sources.User,
            cancellationToken);

        return MapMilestone(milestone);
    }

    private static ProjectListItemDto MapProject(Project project)
    {
        var done = project.Milestones
            .Where(milestone => milestone.Status == MilestoneStatus.Done)
            .Sum(milestone => milestone.StoryPoints);

        return new ProjectListItemDto(
            project.Id,
            project.Name,
            project.Manager.FullName,
            project.EndDate,
            project.Status.ToString(),
            done,
            project.TotalStoryPoints);
    }

    private static MilestoneDto MapMilestone(Milestone milestone) =>
        new(milestone.Id, milestone.Title, milestone.DueDate, milestone.StoryPoints, milestone.Status.ToString());
}
