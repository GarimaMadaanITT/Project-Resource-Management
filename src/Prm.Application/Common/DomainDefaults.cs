using Prm.Domain.Enums;

namespace Prm.Application.Common;

public static class DomainDefaults
{
    public const bool ForcePasswordChangeOnCreate = true;
    public const bool ForcePasswordChangeOnReset = true;

    public static readonly EmployeeStatus NewEmployeeStatus = EmployeeStatus.Bench;
    public static readonly HealthStatus NewProjectHealth = HealthStatus.OnTrack;
    public static readonly MilestoneStatus NewMilestoneStatus = MilestoneStatus.NotStarted;
}
