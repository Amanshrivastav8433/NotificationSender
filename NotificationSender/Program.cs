using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationSender.Configuration;
using NotificationSender.Data;
using NotificationSender.Data.Repositories;
using NotificationSender.Email;
using NotificationSender.Services;
using NotificationSender.WhatsApp;
using NotificationSender.Workers;
using Serilog;
using Serilog.Events;

var builder = Host.CreateApplicationBuilder(args);

var downloadsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    "Downloads",
    "NotificationWorkerLogs");

Directory.CreateDirectory(downloadsPath);

var errorLogsPath = Path.Combine(downloadsPath, "errors");
Directory.CreateDirectory(errorLogsPath);

var logFilePath = Path.Combine(
    downloadsPath,
    "notification-worker-.log");

Log.Logger = new LoggerConfiguration()

    // Default application logs
    .MinimumLevel.Information()

    // Suppress unnecessary Microsoft framework logs
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)

    // Keep important hosting lifecycle logs
    .MinimumLevel.Override(
        "Microsoft.Hosting.Lifetime",
        LogEventLevel.Information)

    // Suppress EF Core SQL queries
    .MinimumLevel.Override(
        "Microsoft.EntityFrameworkCore",
        LogEventLevel.Warning)

    .MinimumLevel.Override(
        "Microsoft.EntityFrameworkCore.Database.Command",
        LogEventLevel.Warning)

    .WriteTo.Console()

    .WriteTo.File(
        logFilePath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true)

    .WriteTo.File(
        Path.Combine(
            errorLogsPath,
            "error-.log"),
        restrictedToMinimumLevel: LogEventLevel.Error,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true)

    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection(EmailSettings.SectionName));

builder.Services.Configure<WhatsAppSettings>(
    builder.Configuration.GetSection(WhatsAppSettings.SectionName));

builder.Services.Configure<WorkerSettings>(
    builder.Configuration.GetSection(WorkerSettings.SectionName));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");
}

builder.Services.AddDbContext<NotificationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationProcessor, NotificationProcessor>();
builder.Services.AddScoped<IEmailSender, GmailEmailSender>();

builder.Services.AddHttpClient<IWhatsAppSender, WhatsAppSender>(
    (serviceProvider, client) =>
    {
        var settings = serviceProvider
            .GetRequiredService<
                Microsoft.Extensions.Options.IOptions<WhatsAppSettings>>()
            .Value;

        client.Timeout = TimeSpan.FromSeconds(
            settings.TimeoutSeconds > 0
                ? settings.TimeoutSeconds
                : 30);
    });

builder.Services.AddHostedService<NotificationWorker>();

try
{
    Log.Information("Notification Sender Worker application is starting.");

    Log.Information(
        "Logs are being saved at: {LogPath}",
        downloadsPath);

    var host = builder.Build();

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(
        ex,
        "Notification Sender Worker terminated unexpectedly.");

    throw;
}
finally
{
    Log.CloseAndFlush();
}