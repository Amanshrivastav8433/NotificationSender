using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationSender.Configuration;
using NotificationSender.Data.Repositories;
using NotificationSender.Email;
using NotificationSender.Models;
using NotificationSender.WhatsApp;

namespace NotificationSender.Services;

public class NotificationProcessor : INotificationProcessor
{
    private readonly INotificationRepository _repository;
    private readonly IEmailSender _emailSender;
    private readonly IWhatsAppSender _whatsAppSender;
    private readonly ILogger<NotificationProcessor> _logger;
    private readonly WorkerSettings _workerSettings;

    public NotificationProcessor(INotificationRepository repository, IEmailSender emailSender, IWhatsAppSender whatsAppSender, IOptions<WorkerSettings> workerOptions, ILogger<NotificationProcessor> logger)
    {
        _repository = repository;
        _emailSender = emailSender;
        _whatsAppSender = whatsAppSender;
        _logger = logger;
        _workerSettings = workerOptions.Value;
    }

    public async Task ProcessPendingNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var batchSize = _workerSettings.BatchSize;
        if (batchSize <= 0)
        {
            batchSize = 50;
        }

        var notifications = await _repository.ClaimPendingNotificationsAsync(batchSize, cancellationToken);
        if (!notifications.Any())
        {
            _logger.LogDebug("No pending notifications found.");

            return;
        }
        _logger.LogInformation("Processing {Count} notifications.", notifications.Count);
        var maxParallelProcessing = _workerSettings.MaxParallelProcessing;
        if (maxParallelProcessing <= 0)
        {
            maxParallelProcessing = 5;
        }
        using var semaphore = new SemaphoreSlim(maxParallelProcessing);
        var tasks = notifications.Select(async notification =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await ProcessSingleNotificationAsync(notification, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
        _logger.LogInformation("Completed processing batch of {Count} notifications.", notifications.Count);
    }

    private async Task ProcessSingleNotificationAsync(NotificationQueue notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing notification {NotificationId}. Type: {NotificationType}", notification.Id, notification.NotificationType);
            switch (notification.NotificationType)
            {
                case NotificationType.Email:
                case NotificationType.OtpEmail:
                    await SendEmailAsync(notification, cancellationToken);
                    break;
                case NotificationType.WhatsApp:
                case NotificationType.OtpWhatsApp:
                    await SendWhatsAppAsync(notification, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported notification type: {notification.NotificationType}");
            }
            await _repository.MarkAsSentAsync(notification.Id, cancellationToken);
            _logger.LogInformation("Notification {NotificationId} sent successfully.", notification.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Notification processing cancelled for {NotificationId}.", notification.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification {NotificationId} failed.", notification.Id);
            try
            {
                await _repository.MarkAsFailedAsync(notification.Id, ex.Message, cancellationToken);
            }
            catch (Exception repositoryException)
            {
                _logger.LogCritical(repositoryException, "Failed to update failure status for notification {NotificationId}.", notification.Id);
            }
        }
    }

    private async Task SendEmailAsync(NotificationQueue notification, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(notification.Recipient))
        {
            throw new InvalidOperationException($"Notification {notification.Id} has no email recipient.");
        }
        await _emailSender.SendAsync(notification.Recipient, notification.Subject ?? string.Empty, notification.Body, cancellationToken);
    }

    private async Task SendWhatsAppAsync(NotificationQueue notification, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(notification.Recipient))
        {
            throw new InvalidOperationException($"Notification {notification.Id} has no WhatsApp recipient.");
        }
        await _whatsAppSender.SendAsync(notification.Recipient, notification.Body, cancellationToken);
    }
}