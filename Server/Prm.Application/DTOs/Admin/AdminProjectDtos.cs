namespace Prm.Application.DTOs.Admin;

public record ProjectListItemDto(
    int Id,
    string Name,
    string ManagerName,
    DateOnly EndDate,
    string Status,
    int StoryPointsDone,
    int TotalStoryPoints);

public record CreateProjectRequest(
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int ManagerUserId,
    int TotalStoryPoints);

public record UpdateProjectRequest(
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int ManagerUserId,
    int TotalStoryPoints);

public record MilestoneDto(
    int Id,
    string Title,
    DateOnly DueDate,
    int StoryPoints,
    string Status);

public record AddMilestoneRequest(
    string Title,
    DateOnly DueDate,
    int StoryPoints);

public record UpdateMilestoneStatusRequest(string Status);
