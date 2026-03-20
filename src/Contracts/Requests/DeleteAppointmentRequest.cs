namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на удаление личного события
/// Используется в: Calendar API
/// </summary>
public sealed record DeleteAppointmentRequest(Guid AppointmentId);