using Calendar.Contracts.Dtos;
using Calendar.Contracts.Types;

namespace Calendar_Service.Services;

/// <summary>
/// Публикует команды бронирования комнат в Booking API (через RabbitMQ)
/// для события и (при наличии правила повторения) каждого его вхождения.
/// </summary>
public interface IBookingPublisher
{
    /// <summary>
    /// Публикует команды бронирования комнаты для события.
    /// Если <paramref name="dto"/>.RoomId равен null — ничего не делает.
    /// </summary>
    Task PublishForAppointmentAsync(AppointmentDto dto, TimeSlot masterSlot, CancellationToken token = default);
}
