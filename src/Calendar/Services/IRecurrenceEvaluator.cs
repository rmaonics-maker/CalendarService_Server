using Calendar_Service.Mapping;
using Calendar_Service.Models;
using Calendar.Contracts.Responses;
using Calendar.Contracts.Types;

namespace Calendar_Service.Services;

public interface IRecurrenceEvaluator
{
    AppointmentOccurrencesResponse GetOccurrences(
        Appointment master,
        List<AppointmentException> exceptions,
        TimeSlot range);
}