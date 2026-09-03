using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationSender.Configuration;

namespace NotificationSender.Email;

public class GmailEmailSender : IEmailSender
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<GmailEmailSender> _logger;

    public GmailEmailSender(IOptions<EmailSettings> emailOptions, ILogger<GmailEmailSender> logger)
    {
        _emailSettings = emailOptions.Value;
        _logger = logger;
    }

    public async Task SendAsync(string recipientEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new ArgumentException("Recipient email address is required.", nameof(recipientEmail));
        }

        if (string.IsNullOrWhiteSpace(_emailSettings.SmtpHost))
        {
            throw new InvalidOperationException("SMTP host is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_emailSettings.Username))
        {
            throw new InvalidOperationException("SMTP username is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_emailSettings.Password))
        {
            throw new InvalidOperationException("SMTP password is not configured.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress( _emailSettings.SenderName, _emailSettings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Subject = subject ?? string.Empty;
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body ?? string.Empty,
            TextBody = body ?? string.Empty
        };
        message.Body = bodyBuilder.ToMessageBody();
        using var smtpClient = new SmtpClient();

        try
        {
            await smtpClient.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
            await smtpClient.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);
            await smtpClient.SendAsync(message, cancellationToken);
            _logger.LogInformation("Email successfully sent to {RecipientEmail}", recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {RecipientEmail}", recipientEmail);
            throw;
        }
        finally
        {
            if (smtpClient.IsConnected)
            {
                await smtpClient.DisconnectAsync(true, cancellationToken);
            }
        }
    }
}