using System.Diagnostics.Metrics;

namespace Calendar_Service.Monitoring;

public static class Instrumentation
{
    public const string MeterName = "Calendar.Api";
    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> AppointmentsCreatedCounter = Meter.CreateCounter<long>(
        "calendar_appointments_created_total",
        description: "Total number of appointments created");

    public static readonly Histogram<double> AppointmentProcessingDuration = Meter.CreateHistogram<double>(
        "calendar_appointment_processing_duration_ms",
        unit: "ms",
        description: "Duration of appointment processing");
}
