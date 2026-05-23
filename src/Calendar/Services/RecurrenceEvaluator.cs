using Calendar_Service.Models;
using Calendar.Contracts.Responses;
using Calendar.Contracts.Types;
using Calendar.Contracts.Dtos;
using NodaTime;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using AutoMapper;

namespace Calendar_Service.Services;

public class RecurrenceEvaluator : IRecurrenceEvaluator
{
    private readonly IMapper _mapper;

    public RecurrenceEvaluator(IMapper mapper)
    {
        _mapper = mapper;
    }

    public AppointmentOccurrencesResponse GetOccurrences(
        Appointment master, 
        List<AppointmentException> exceptions,
        TimeSlot range)
    {
        var rawInstants = ExpandRule(master, range);
        var exceptionMap = exceptions.ToDictionary(e => e.OriginalOccurrenceDate);
        var results = new List<AppointmentDto>();

        foreach (var date in rawInstants)
        {
            var dto = _mapper.Map<AppointmentDto>(master, opt => opt.Items["OccurrenceDate"] = date);

            if (exceptionMap.TryGetValue(date, out var exception))
            {
                if (exception.IsCancelled) continue;

                dto = dto with 
                { 
                    IsException = true,
                    Title = exception.OverrideTitle ?? dto.Title,
                    StartTime = exception.OverrideSlot?.Start.ToDateTimeOffset() ?? dto.StartTime,
                    Duration = exception.OverrideSlot != null 
                        ? (exception.OverrideSlot.Value.End - exception.OverrideSlot.Value.Start).ToTimeSpan() 
                        : dto.Duration
                };
            }

            results.Add(dto);
        }

        return new AppointmentOccurrencesResponse(results);
    }

    private List<Instant> ExpandRule(Appointment master, TimeSlot range)
    {
        if (string.IsNullOrWhiteSpace(master.Recurrence?.RRule))
        {
            return (master.Slot.Start >= range.Start && master.Slot.Start <= range.End)
                ? new List<Instant> { master.Slot.Start }
                : new List<Instant>();
        }

        var calendar = new Ical.Net.Calendar();
        var calendarEvent = new CalendarEvent
        {
            Start = new CalDateTime(master.Slot.Start.ToDateTimeUtc())
        };

        calendarEvent.RecurrenceRules.Add(new Ical.Net.DataTypes.RecurrencePattern(master.Recurrence.RRule));
        calendar.Events.Add(calendarEvent);

        var searchStart = new CalDateTime(range.Start.ToDateTimeUtc());
        var searchEndUtc = range.End.ToDateTimeUtc();

        return calendar.GetOccurrences(searchStart)
            .TakeWhile(occ => occ.Period.StartTime.AsUtc <= searchEndUtc)
            .Select(occ => Instant.FromDateTimeUtc(occ.Period.StartTime.AsUtc))
            .ToList();
    }
}
