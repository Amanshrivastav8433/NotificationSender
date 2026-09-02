using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NotificationSender.Workers;

public class NotificationWorker : BackgroundService
{
    private readonly IConfiguration _configuration;

    public NotificationWorker(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "Notification Worker Started");

        Console.WriteLine(
            "====================================");

        var intervalSeconds =
            _configuration.GetValue<int>(
                "WorkerSettings:IntervalSeconds");

        if (intervalSeconds <= 0)
        {
            intervalSeconds = 10;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine(
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Checking database...");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Worker Error: {ex.Message}");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(intervalSeconds),
                stoppingToken);
        }

        Console.WriteLine(
            "Notification Worker Stopped");
    }
}