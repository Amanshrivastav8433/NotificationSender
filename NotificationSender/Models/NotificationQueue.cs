namespace NotificationSender.Models;

public class NotificationQueue
{
    public long Id { get; set; }
    public NotificationType NotificationType { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public int MaxRetryCount { get; set; } = 3;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
}