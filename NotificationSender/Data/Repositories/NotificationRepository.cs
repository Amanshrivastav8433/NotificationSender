using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationSender.Configuration;
using NotificationSender.Models;

namespace NotificationSender.Data.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<NotificationRepository> _logger;
    private readonly WorkerSettings _workerSettings;

    public NotificationRepository(NotificationDbContext context, ILogger<NotificationRepository> logger, IOptions<WorkerSettings> workerOptions)
    {
        _context = context;
        _logger = logger;
        _workerSettings = workerOptions.Value;
    }

    /// <summary>
    /// Safely claims pending notifications.
    /// Uses PostgreSQL:
    /// FOR UPDATE SKIP LOCKED
    /// This prevents multiple worker instances from processing
    /// the same notification simultaneously.
    /// </summary>
    public async Task<List<NotificationQueue>> ClaimPendingNotificationsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            return new List<NotificationQueue>();
        }
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var notifications = await _context.NotificationQueue
            .FromSqlInterpolated($@"
                SELECT *
                FROM notification_queue
                WHERE status = {(int)NotificationStatus.Pending}
                  AND retry_count < max_retry_count
                ORDER BY created_at
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
            ")
            .ToListAsync(cancellationToken);
        if (!notifications.Any())
        {
            await transaction.CommitAsync(cancellationToken);
            return notifications;
        }

        var now = DateTime.UtcNow;
        foreach (var notification in notifications)
        {
            notification.Status = NotificationStatus.Processing;
            notification.ProcessingStartedAt = now;
            notification.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation("{Count} notifications claimed for processing", notifications.Count);
        return notifications;
    }


    /// <summary>
    /// Marks notification as successfully sent.
    /// </summary>
    public async Task MarkAsSentAsync(long notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.NotificationQueue.FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);
        if (notification == null)
        {
            _logger.LogWarning("Notification {NotificationId} not found while marking as sent", notificationId);
            return;
        }

        var now = DateTime.UtcNow;
        notification.Status = NotificationStatus.Sent;
        notification.SentAt = now;
        notification.UpdatedAt = now;
        notification.ProcessingStartedAt = null;
        notification.ErrorMessage = null;
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Notification {NotificationId} marked as sent", notificationId);
    }


    /// <summary>
    /// Handles notification sending failure.
    /// </summary>
    public async Task MarkAsFailedAsync(long notificationId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var notification = await _context.NotificationQueue.FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);

        if (notification == null)
        {
            _logger.LogWarning("Notification {NotificationId} not found while marking as failed", notificationId);
            return;
        }

        var now = DateTime.UtcNow;
        notification.RetryCount++;
        notification.ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Unknown error" : errorMessage.Length > 4000 ? errorMessage[..4000] : errorMessage;
        notification.UpdatedAt = now;
        notification.ProcessingStartedAt = null;

        if (notification.RetryCount >= notification.MaxRetryCount)
        {
            notification.Status = NotificationStatus.Failed;
            _logger.LogError("Notification {NotificationId} permanently failed after {RetryCount} attempts", notification.Id, notification.RetryCount);
        }
        else
        {
            notification.Status = NotificationStatus.Pending;
            _logger.LogWarning("Notification {NotificationId} failed. It will be retried. Attempt {RetryCount}/{MaxRetryCount}", notification.Id, notification.RetryCount, notification.MaxRetryCount);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }


    /// <summary>
    /// Resets notifications stuck in Processing state.
    /// This handles worker crashes, restarts, or unexpected shutdowns.
    /// </summary>
    public async Task ResetStuckNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var timeoutMinutes = _workerSettings.ProcessingTimeoutMinutes;

        if (timeoutMinutes <= 0)
        {
            timeoutMinutes = 10;
        }
        var threshold = DateTime.UtcNow.AddMinutes(-timeoutMinutes);

        var stuckNotifications = await _context.NotificationQueue
            .Where(x =>
                x.Status == NotificationStatus.Processing &&
                x.ProcessingStartedAt != null &&
                x.ProcessingStartedAt < threshold)
            .ToListAsync(cancellationToken);

        if (!stuckNotifications.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;

        foreach (var notification in stuckNotifications)
        {
            notification.Status = NotificationStatus.Pending;
            notification.ProcessingStartedAt = null;
            notification.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("{Count} stuck notifications reset to Pending",  stuckNotifications.Count);
    }
}