using Calendar_Service.Mapping;
using Calendar_Service.Models;
using Calendar.Contracts.Responses;
using Calendar.Contracts.Types;
using Calendar.Contracts.Dtos;
using NodaTime;

namespace Calendar_Service.Services;

public interface IRecurrenceEvaluator
{
    AppointmentOccurrencesResponse GetOccurrences(
        Appointment master,
        List<AppointmentException> exceptions,
        TimeSlot range);

    /// <summary>
    /// Раскрывает повторяющееся правило вхождения в список слотов [start, end].
    /// Для неповторяющегося события (recurrence == null) возвращает один слот masterSlot,
    /// если он попадает в диапазон [from, to].
    /// </summary>
    /// <param name="masterSlot">Слот мастера [start, end].</param>
    /// <param name="recurrence">Правило повторения (null — неповторяющееся).</param>
    /// <param name="from">Нижняя граница диапазона.</param>
    /// <param name="to">Верхняя граница диапазона.</param>
    /// <param name="maxOccurrences">Защита от бесконечных правил.</param>
    /// <returns>Список слотов вхождений с длительностью masterSlot.</returns>
    IReadOnlyList<TimeSlot> ExpandOccurrences(
        TimeSlot masterSlot,
        RecurrencePatternDto? recurrence,
        Instant from,
        Instant to,
        int maxOccurrences);
}