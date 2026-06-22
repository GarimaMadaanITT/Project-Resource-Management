using Prm.Application.DTOs.Notifications;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Notifications;

public interface IProjectAtRiskNotificationService
{
    Task NotifyAsync(
        Project project,
        IReadOnlyList<ProjectRiskFlagCalculator.RiskFlag> riskFlags,
        HealthStatus previousHealth,
        CancellationToken cancellationToken = default);
}

public class ProjectAtRiskNotificationService : IProjectAtRiskNotificationService
{
    private readonly IUserRepository _users;
    private readonly IProjectRiskContentBuilder _riskContentBuilder;
    private readonly AtRiskSkillSuggestionService _skillSuggestions;
    private readonly NotificationDispatchService _notificationDispatch;

    public ProjectAtRiskNotificationService(
        IUserRepository users,
        IProjectRiskContentBuilder riskContentBuilder,
        AtRiskSkillSuggestionService skillSuggestions,
        NotificationDispatchService notificationDispatch)
    {
        _users = users;
        _riskContentBuilder = riskContentBuilder;
        _skillSuggestions = skillSuggestions;
        _notificationDispatch = notificationDispatch;
    }

    public async Task NotifyAsync(
        Project project,
        IReadOnlyList<ProjectRiskFlagCalculator.RiskFlag> riskFlags,
        HealthStatus previousHealth,
        CancellationToken cancellationToken = default)
    {
        if (project.HealthStatus != HealthStatus.AtRisk || previousHealth == HealthStatus.AtRisk)
        {
            return;
        }

        var manager = await _users.GetByIdAsync(project.ManagerUserId, cancellationToken);
        if (manager is null || string.IsNullOrWhiteSpace(manager.Email))
        {
            return;
        }

        var referenceKey = $"project:{project.Id}:atrisk:{ActiveDateHelper.TodayUtc:yyyy-MM-dd}";

        var summary = await _riskContentBuilder.BuildSummaryAsync(project, riskFlags, cancellationToken);
        var requirement = AtRiskSkillRequirementDeriver.Derive(riskFlags);
        var suggestedEmployees = await _skillSuggestions.GetSuggestedEmployeesAsync(requirement, cancellationToken);

        var milestoneLines = project.Milestones
            .OrderBy(milestone => milestone.DueDate)
            .Select(milestone => $"{milestone.Title} — due {milestone.DueDate:dd-MMM-yyyy}, {milestone.Status}")
            .ToList();

        var subject = EmailTemplateBuilder.ProjectAtRiskSubject(project.Name);
        var body = EmailTemplateBuilder.BuildProjectAtRisk(
            project.Name,
            manager.FullName,
            project.HealthStatus,
            milestoneLines,
            summary,
            suggestedEmployees);

        await _notificationDispatch.SendIfNotSentAsync(
            NotificationType.ProjectAtRisk,
            referenceKey,
            new EmailMessage(manager.Email, subject, body),
            cancellationToken);
    }
}
