namespace Calendar.Contracts.Events;
/// <summary>
/// Событие: бронирования удалены
/// </summary>
public sealed record BookingsDeletedEvent(
    Guid AppointmentId,
    int Count
);