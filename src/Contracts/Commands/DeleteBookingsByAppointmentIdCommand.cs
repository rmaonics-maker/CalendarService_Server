namespace Calendar.Contracts.Commands;
/// <summary>
/// Команда: удалить бронирования по ID appointment
/// Отправляется: Calendar API → Booking API
/// Используется для каскадного удаления бронирований при удалении appointment
/// </summary>
public sealed record DeleteBookingsByEventIdCommand(Guid AppointmentId);