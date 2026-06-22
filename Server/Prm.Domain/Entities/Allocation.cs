using Prm.Domain.Common;

namespace Prm.Domain.Entities;

public class Allocation : AuditableEntity
{
    public int ResourceProfileId { get; set; }
    public int ProjectId { get; set; }
    public int UtilisationPercent { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }

    public ResourceProfile ResourceProfile { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
