namespace NotificationSender.Configuration;

public class WhatsAppSettings
{
    public const string SectionName = "WhatsAppSettings";
    public string BaseUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}