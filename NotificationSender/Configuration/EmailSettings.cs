namespace NotificationSender.Configuration
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        public string Username { get; set; } = string.Empty;

        public string AppPassword { get; set; } = string.Empty;

        public string FromName { get; set; } = string.Empty;
    }
}