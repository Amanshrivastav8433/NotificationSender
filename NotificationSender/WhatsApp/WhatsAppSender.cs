using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationSender.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NotificationSender.WhatsApp;

public class WhatsAppSender : IWhatsAppSender
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppSettings _whatsAppSettings;
    private readonly ILogger<WhatsAppSender> _logger;

    public WhatsAppSender(HttpClient httpClient, IOptions<WhatsAppSettings> whatsAppOptions, ILogger<WhatsAppSender> logger)
    {
        _httpClient = httpClient;
        _whatsAppSettings = whatsAppOptions.Value;
        _logger = logger;
    }

    public async Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        ValidateSettings();
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("WhatsApp phone number is required.", nameof(phoneNumber));
        }
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("WhatsApp message is required.", nameof(message));
        }
        var requestUrl = $"{_whatsAppSettings.BaseUrl.TrimEnd('/')}/" + $"{_whatsAppSettings.ApiVersion}/" + $"{_whatsAppSettings.PhoneNumberId}/messages";
        var requestBody = new
        {
            messaging_product = "whatsapp",
            to = NormalizePhoneNumber(phoneNumber),
            type = "text",
            text = new
            {
                preview_url = false,
                body = message
            }
        };
        using var request = new HttpRequestMessage( HttpMethod.Post, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _whatsAppSettings.AccessToken);
        request.Content = JsonContent.Create(requestBody);
        _logger.LogInformation("Sending WhatsApp message to {PhoneNumber}", phoneNumber);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync( cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("WhatsApp API failed. StatusCode: {StatusCode}, Response: {Response}", response.StatusCode, responseContent);
            throw new HttpRequestException($"WhatsApp API request failed. " + $"StatusCode: {(int)response.StatusCode}. " + $"Response: {responseContent}");
        }
        _logger.LogInformation("WhatsApp message successfully sent to {PhoneNumber}", phoneNumber);
    }


    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_whatsAppSettings.BaseUrl))
        {
            throw new InvalidOperationException("WhatsApp BaseUrl is not configured.");
        }
        if (string.IsNullOrWhiteSpace(_whatsAppSettings.ApiVersion))
        {
            throw new InvalidOperationException("WhatsApp API version is not configured.");
        }
        if (string.IsNullOrWhiteSpace(_whatsAppSettings.PhoneNumberId))
        {
            throw new InvalidOperationException("WhatsApp PhoneNumberId is not configured.");
        }
        if (string.IsNullOrWhiteSpace(_whatsAppSettings.AccessToken))
        {
            throw new InvalidOperationException("WhatsApp AccessToken is not configured.");
        }
    }
    private static string NormalizePhoneNumber(string phoneNumber)
    {
        // WhatsApp Cloud API expects:
        // Country code + phone number
        // Example:
        // 919876543210
        return phoneNumber.Replace("+", string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty).Trim();
    }
}