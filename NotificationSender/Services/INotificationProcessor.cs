namespace NotificationSender.Services;

public interface INotificationProcessor
{
    Task ProcessPendingNotificationsAsync(CancellationToken cancellationToken = default);
}