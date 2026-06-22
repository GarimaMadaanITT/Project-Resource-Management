using Microsoft.Extensions.Logging;
using Prm.Application.Interfaces;

namespace Prm.Application.Services.Scheduler;

public class SchedulerOrchestrator : ISchedulerOrchestrator
{
    private readonly IUtilisationRecomputeService _utilisation;
    private readonly IProjectHealthRecomputeService _projectHealth;
    private readonly ITimesheetMissedDetectionService _missedTimesheets;
    private readonly ILogger<SchedulerOrchestrator> _logger;

    public SchedulerOrchestrator(
        IUtilisationRecomputeService utilisation,
        IProjectHealthRecomputeService projectHealth,
        ITimesheetMissedDetectionService missedTimesheets,
        ILogger<SchedulerOrchestrator> logger)
    {
        _utilisation = utilisation;
        _projectHealth = projectHealth;
        _missedTimesheets = missedTimesheets;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scheduler run started.");

        try
        {
            await _utilisation.RunAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Utilisation recompute job failed.");
        }

        try
        {
            await _projectHealth.RunAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Project health recompute job failed.");
        }

        try
        {
            await _missedTimesheets.RunAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Missed timesheet detection job failed.");
        }

        _logger.LogInformation("Scheduler run completed.");
    }
}
