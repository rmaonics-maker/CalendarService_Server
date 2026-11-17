using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Calendar.Booking.API.Services;

public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new("Calendar.Booking.API");
    public static readonly Meter Meter = new("Calendar.Booking.API", "1.0.0");

    public static readonly Counter<long> LockAcquiredCount = Meter.CreateCounter<long>(
        "booking.lock.acquired", "count", "Количество успешных захватов блокировки");

    public static readonly Counter<long> LockFailedCount = Meter.CreateCounter<long>(
        "booking.lock.failed", "count", "Количество неудачных попыток блокировки");

    public static readonly Histogram<double> LockWaitDuration =
        Meter.CreateHistogram<double>(
            "booking.lock.wait_duration", "ms", "Время ожидания блокировки");

    public static readonly Histogram<double> BookingCreationDuration =
        Meter.CreateHistogram<double>(
            "booking.creation.duration", "ms", "Полное время создания бронирования");

    // Имена инструментов — без "_total" в конце и без юнита "count": иначе OTel Prometheus
    // exporter и Prometheus 3 навешивают суффиксы дважды (booking_created_total_count_total)
    // и нормализуют их по-разному, из-за чего дашборд ловит No data.
    public static readonly Counter<long> BookingsCreatedCounter = Meter.CreateCounter<long>(
        "bookings_created", description: "Total number of bookings created");

    public static readonly Counter<long> BookingConflictCounter = Meter.CreateCounter<long>(
        "booking_conflicts", description: "Total number of booking conflicts (Double Booking)");
}