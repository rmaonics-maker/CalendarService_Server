namespace Calendar.Contracts.Commands;
/// <summary>
/// Команда: удалить бронирования по ID события
/// </summary>
public sealed record DeleteBookingsByEventIdCommand(Guid AppointmentId);