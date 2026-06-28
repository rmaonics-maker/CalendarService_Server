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
        return ExpandInstants(master.Slot, master.Recurrence != null
            ? new RecurrencePatternDto(master.Recurrence.RRule, master.Recurrence.Until, master.Recurrence.Count)
            : null, range.Start, range.End, int.MaxValue);
    }

    /// <inheritdoc />
    public IReadOnlyList<TimeSlot> ExpandOccurrences(
        TimeSlot masterSlot,
        RecurrencePatternDto? recurrence,
        Instant from,
        Instant to,
        int maxOccurrences)
    {
        var duration = masterSlot.End - masterSlot.Start;
        var instants = ExpandInstants(masterSlot, recurrence, from, to, maxOccurrences);

        return instants
            .Select(start => new TimeSlot(start, start.Plus(duration)))
            .ToList();
    }

    private List<Instant> ExpandInstants(
        TimeSlot masterSlot,
        RecurrencePatternDto? recurrence,
        Instant from,
        Instant to,
        int maxOccurrences)
    {
        if (recurrence is null || string.IsNullOrWhiteSpace(recurrence.RRule))
        {
            // Неповторяющееся событие — одно вхождение, если попадает в диапазон.
            return (masterSlot.Start >= from && masterSlot.Start <= to)
                ? new List<Instant> { masterSlot.Start }
                : new List<Instant>();
        }

        var calendar = new Ical.Net.Calendar();
        var calendarEvent = new CalendarEvent
        {
            Start = new CalDateTime(masterSlot.Start.ToDateTimeUtc())
        };

        calendarEvent.RecurrenceRules.Add(new Ical.Net.DataTypes.RecurrencePattern(recurrence.RRule));
        calendar.Events.Add(calendarEvent);

        var searchStart = new CalDateTime(from.ToDateTimeUtc());
        var searchEndUtc = to.ToDateTimeUtc();

        return calendar.GetOccurrences(searchStart)
            .TakeWhile(occ => occ.Period.StartTime.AsUtc <= searchEndUtc)
            .Take(maxOccurrences)
            .Select(occ => Instant.FromDateTimeUtc(occ.Period.StartTime.AsUtc))
            .ToList();
    }
}
