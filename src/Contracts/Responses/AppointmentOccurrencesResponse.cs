using Calendar.Contracts.Dtos;

namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ со списком событий за период
/// Используется в: Calendar API
/// </summary>
public sealed record AppointmentOccurrencesResponse(List<AppointmentDto> Events);