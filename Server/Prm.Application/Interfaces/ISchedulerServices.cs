using Prm.Application.DTOs.Admin;

namespace Prm.Application.Interfaces;

public interface ISchedulerOrchestrator
{
    Task RunAsync(CancellationToken cancellationToken = default);
}

public interface IUtilisationRecomputeService
{
    Task<int> RunAsync(CancellationToken cancellationToken = default);
}

public interface IProjectHealthRecomputeService
{
    Task<int> RunAsync(CancellationToken cancellationToken = default);
}

public interface ITimesheetMissedDetectionService
{
    Task<int> RunAsync(CancellationToken cancellationToken = default);

    Task<TimesheetComplianceForceResponse> ForceAdvanceComplianceAsync(
        string username,
        CancellationToken cancellationToken = default);
}
