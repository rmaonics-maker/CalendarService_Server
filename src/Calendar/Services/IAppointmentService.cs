using Calendar.Contracts.Dtos;
using Calendar_Service.Models;

namespace Calendar_Service.Services;

public interface IAppointmentService
{
    Task<AppointmentDto> CreateAppointmentAsync(
        AppointmentDto evt,
        CancellationToken token = default);
    
    Task<AppointmentDto?> GetAppointmentAsync(Guid id, CancellationToken token = default);
    
    /// <summary>
    /// Возвращает развернутый список всех вхождений (включая повторения и исключения) за указанный период.
    /// </summary>
    Task<IEnumerable<AppointmentDto>> GetAppointmentsAsync(
        DateTimeOffset from,
        DateTimeOffset to, CancellationToken token = default);
    
    Task<AppointmentDto?> UpdateAppointmentAsync(
        Guid id,
        AppointmentDto request,
        CancellationToken token = default);
    
    Task<bool> DeleteAppointmentAsync(
        Guid id,
        CancellationToken token = default);

    Task<List<AppointmentException>> GetExceptionsAsync(Guid masterId, CancellationToken token = default);
}