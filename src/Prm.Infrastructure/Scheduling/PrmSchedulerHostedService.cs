using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prm.Application.Interfaces;

namespace Prm.Infrastructure.Scheduling;

public class PrmSchedulerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PrmSchedulerHostedService> _logger;

    public PrmSchedulerHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<PrmSchedulerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCycleAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = await GetDelayAsync(stoppingToken);
            await Task.Delay(delay, stoppingToken);
            await RunCycleAsync(stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<ISchedulerOrchestrator>();
            await orchestrator.RunAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Scheduler cycle failed.");
        }
    }

    private async Task<TimeSpan> GetDelayAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISystemSettingsRepository>();
        var systemSettings = await settings.GetAsync(cancellationToken);
        var hours = systemSettings.SchedulerIntervalHours <= 0 ? 4 : systemSettings.SchedulerIntervalHours;
        return TimeSpan.FromHours(hours);
    }
}
