using NotificationSender.Models;

namespace NotificationSender.Data.Repositories;

public interface INotificationRepository
{
    /// <summary>
    /// Claims pending notifications for processing.
    /// PostgreSQL row locking prevents multiple worker instances
    /// from processing the same notification.
    /// </summary>
    Task<List<NotificationQueue>> ClaimPendingNotificationsAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a notification as successfully sent.
    /// </summary>
    Task MarkAsSentAsync(long notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a failed notification attempt.
    /// It increments retry count and either returns the notification
    /// to Pending or marks it as Failed.
    /// </summary>
    Task MarkAsFailedAsync(long notificationId, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets notifications that were stuck in Processing state,
    /// for example after a worker crash or application restart.
    /// </summary>
    Task ResetStuckNotificationsAsync(CancellationToken cancellationToken = default);
}