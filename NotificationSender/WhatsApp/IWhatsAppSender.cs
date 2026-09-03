namespace NotificationSender.WhatsApp;

public interface IWhatsAppSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}