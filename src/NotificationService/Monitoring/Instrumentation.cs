using System.Diagnostics.Metrics;

namespace NotificationService.Monitoring;

public static class Instrumentation
{
    public const string MeterName = "Calendar.Worker";
    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> NotificationsSentCounter = Meter.CreateCounter<long>(
        "notifications_sent_total",
        description: "Total number of notifications sent");
}
