namespace NotificationSender.Configuration;

public class WorkerSettings
{
    public const string SectionName = "WorkerSettings";
    public int PollingIntervalSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 50;
    public int ProcessingTimeoutMinutes { get; set; } = 10;
    public int MaxParallelProcessing { get; set; } = 5;
}