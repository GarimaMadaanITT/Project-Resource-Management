using Prm.Domain.Enums;

namespace Prm.Application.Common;

public static class DomainDefaults
{
    public const bool IsTemporaryPasswordOnCreate = true;
    public const bool IsTemporaryPasswordOnReset = true;

    public static readonly ResourceStatus NewResourceStatus = ResourceStatus.Bench;
    public static readonly HealthStatus NewProjectHealth = HealthStatus.OnTrack;
    public static readonly MilestoneStatus NewMilestoneStatus = MilestoneStatus.NotStarted;
}
