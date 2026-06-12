using Prm.Domain.Common;
using Prm.Domain.Enums;

namespace Prm.Domain.Entities;

public class Milestone : AuditableEntity
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public int StoryPoints { get; set; }
    public MilestoneStatus Status { get; set; } = MilestoneStatus.NotStarted;

    public Project Project { get; set; } = null!;
}
