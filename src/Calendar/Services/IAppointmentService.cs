using Calendar.Contracts.Dtos;

namespace Calendar_Service.Services;

public interface IAppointmentService
{
    Task<AppointmentDto> CreateAppointmentAsync(
        AppointmentDto evt,
        CancellationToken token = default);
    
    Task<AppointmentDto?> GetAppointmentAsync(Guid id, CancellationToken token = default);
    
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
}