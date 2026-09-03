using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationSender.Configuration;
using NotificationSender.Data.Repositories;
using NotificationSender.Services;

namespace NotificationSender.Workers;

public class NotificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationWorker> _logger;
    private readonly WorkerSettings _workerSettings;

    public NotificationWorker(IServiceScopeFactory scopeFactory, IOptions<WorkerSettings> workerOptions, ILogger<NotificationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _workerSettings = workerOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Sender Worker started at {Time}", DateTime.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotificationsAsync(stoppingToken);
                var pollingInterval = _workerSettings.PollingIntervalSeconds;
                if (pollingInterval <= 0)
                {
                    pollingInterval = 10;
                }
                await Task.Delay(TimeSpan.FromSeconds(pollingInterval), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Notification Sender Worker is stopping.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Notification Worker.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        _logger.LogInformation("Notification Sender Worker stopped at {Time}", DateTime.UtcNow);
    }

    private async Task ProcessNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var processor = scope.ServiceProvider.GetRequiredService<INotificationProcessor>();
        await repository.ResetStuckNotificationsAsync(cancellationToken);
        await processor.ProcessPendingNotificationsAsync(cancellationToken);
    }
}