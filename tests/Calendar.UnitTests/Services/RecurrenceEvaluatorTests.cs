using AutoMapper;
using Calendar_Service.Models;
using Calendar_Service.Services;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Types;
using FluentAssertions;
using NodaTime;
using NSubstitute;

namespace Calendar.UnitTests.Services;

public class RecurrenceEvaluatorTests
{
    private readonly IRecurrenceEvaluator _evaluator;
    private readonly IMapper _mapper;

    public RecurrenceEvaluatorTests()
    {
        _mapper = Substitute.For<IMapper>();
        _evaluator = new RecurrenceEvaluator(_mapper);

        // Mocking the mapping call used in RecurrenceEvaluator to simulate the real mapping logic
        _mapper.Map<AppointmentDto>(Arg.Any<object>(), Arg.Any<Action<IMappingOperationOptions<object, AppointmentDto>>>())
            .Returns(call => {
                var appt = (Appointment)call[0];
                var optionsAction = (Action<IMappingOperationOptions<object, AppointmentDto>>)call[1];
                
                var options = Substitute.For<IMappingOperationOptions<object, AppointmentDto>>();
                var items = new Dictionary<string, object>();
                options.Items.Returns(items);
                optionsAction(options);

                Instant? date = items.TryGetValue("OccurrenceDate", out var d) ? (Instant)d : null;
                
                return new AppointmentDto
                {
                    Id = date.HasValue ? $"{appt.Id}_{date.Value:yyyyMMddTHHmmss'Z'}" : appt.Id.ToString(),
                    MasterId = appt.Id,
                    Title = appt.Title,
                    Description = appt.Description,
                    Location = appt.Location,
                    UserId = appt.UserId,
                    StartTime = date?.ToDateTimeOffset() ?? appt.Slot.Start.ToDateTimeOffset(),
                    Duration = (appt.Slot.End - appt.Slot.Start).ToTimeSpan(),
                    IsException = false
                };
            });
    }

    [Fact]
    public void GetOccurrences_ShouldReturnSingleOccurrence_WhenNoRecurrence()
    {
        // Arrange
        var start = Instant.FromUtc(2026, 5, 23, 10, 0);
        var end = start.Plus(Duration.FromHours(1));
        var master = new Appointment
        {
            Id = Guid.NewGuid(),
            Title = "Single Event",
            Description = "Test",
            Location = "Home",
            DateTimeZoneId = "UTC",
            Slot = new TimeSlot(start, end),
            UserId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var range = new TimeSlot(start.Minus(Duration.FromDays(1)), end.Plus(Duration.FromDays(1)));

        // Act
        var result = _evaluator.GetOccurrences(master, new List<AppointmentException>(), range);

        // Assert
        result.Events.Should().HaveCount(1);
        result.Events[0].Title.Should().Be("Single Event");
        result.Events[0].StartTime.Should().Be(start.ToDateTimeOffset());
    }

    [Fact]
    public void GetOccurrences_ShouldReturnMultipleOccurrences_WhenDailyRecurrence()
    {
        // Arrange
        var start = Instant.FromUtc(2026, 5, 23, 10, 0);
        var end = start.Plus(Duration.FromHours(1));
        var master = new Appointment
        {
            Id = Guid.NewGuid(),
            Title = "Daily Event",
            Description = "Test",
            Location = "Office",
            DateTimeZoneId = "UTC",
            Slot = new TimeSlot(start, end),
            Recurrence = new RecurrencePattern("FREQ=DAILY;COUNT=3", null, 3),
            UserId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var range = new TimeSlot(start, start.Plus(Duration.FromDays(10)));

        // Act
        var result = _evaluator.GetOccurrences(master, new List<AppointmentException>(), range);

        // Assert
        result.Events.Should().HaveCount(3);
        result.Events[0].StartTime.Should().Be(start.ToDateTimeOffset());
        result.Events[1].StartTime.Should().Be(start.Plus(Duration.FromDays(1)).ToDateTimeOffset());
        result.Events[2].StartTime.Should().Be(start.Plus(Duration.FromDays(2)).ToDateTimeOffset());
    }

    [Fact]
    public void GetOccurrences_ShouldApplyException_WhenOneOccurrenceIsOverridden()
    {
        // Arrange
        var start = Instant.FromUtc(2026, 5, 23, 10, 0);
        var master = new Appointment
        {
            Id = Guid.NewGuid(),
            Title = "Daily Event",
            Description = "Test",
            Location = "Office",
            DateTimeZoneId = "UTC",
            Slot = new TimeSlot(start, start.Plus(Duration.FromHours(1))),
            Recurrence = new RecurrencePattern("FREQ=DAILY;COUNT=3", null, 3),
            UserId = Guid.NewGuid()
        };

        var exceptionDate = start.Plus(Duration.FromDays(1));
        var exceptions = new List<AppointmentException>
        {
            new AppointmentException
            {
                Id = Guid.NewGuid(),
                MasterAppointmentId = master.Id,
                OriginalOccurrenceDate = exceptionDate,
                OverrideTitle = "Overridden Title",
                OverrideSlot = new TimeSlot(exceptionDate.Plus(Duration.FromHours(1)), exceptionDate.Plus(Duration.FromHours(2)))
            }
        };

        var range = new TimeSlot(start, start.Plus(Duration.FromDays(10)));

        // Act
        var result = _evaluator.GetOccurrences(master, exceptions, range);

        // Assert
        result.Events.Should().HaveCount(3);
        var overridden = result.Events.First(e => e.IsException);
        overridden.Title.Should().Be("Overridden Title");
        overridden.StartTime.Should().Be(exceptionDate.Plus(Duration.FromHours(1)).ToDateTimeOffset());
        overridden.Id.Should().Contain(exceptionDate.ToString("yyyyMMddTHHmmss'Z'", null));
    }

    [Fact]
    public void GetOccurrences_ShouldSkipOccurrence_WhenCancelled()
    {
        // Arrange
        var start = Instant.FromUtc(2026, 5, 23, 10, 0);
        var master = new Appointment
        {
            Id = Guid.NewGuid(),
            Title = "Daily Event",
            Slot = new TimeSlot(start, start.Plus(Duration.FromHours(1))),
            Recurrence = new RecurrencePattern("FREQ=DAILY;COUNT=3", null, 3),
            UserId = Guid.NewGuid(),
            Description = "Test",
            Location = "Office",
            DateTimeZoneId = "UTC"
        };

        var cancelDate = start.Plus(Duration.FromDays(1));
        var exceptions = new List<AppointmentException>
        {
            new AppointmentException
            {
                Id = Guid.NewGuid(),
                MasterAppointmentId = master.Id,
                OriginalOccurrenceDate = cancelDate,
                IsCancelled = true
            }
        };

        var range = new TimeSlot(start, start.Plus(Duration.FromDays(10)));

        // Act
        var result = _evaluator.GetOccurrences(master, exceptions, range);

        // Assert
        result.Events.Should().HaveCount(2);
        result.Events.Should().NotContain(e => e.StartTime == cancelDate.ToDateTimeOffset());
    }

    [Fact]
    public void ExpandOccurrences_ShouldReturnSingleSlot_WhenNoRecurrence()
    {
        // Arrange
        var start = Instant.FromUtc(2026, 5, 23, 10, 0);
        var end = start.Plus(Duration.FromHours(1));
        var slot = new TimeSlot(start, end);
        var range = new TimeSlot(start.Minus(Duration.FromDays(1)), end.Plus(Duration.FromDays(1)));

        // Act
        var result = _evaluator.ExpandOccurrences(slot, recurrence: null, from: range.Start, to: range.End, maxOccurrences: 10);

        // Assert
        result.Should().HaveCount(1);
        result[0].Start.Should().Be(start);
        result[0].End.Should().Be(end);
    }

    [Fact]
    public void ExpandOccurrences_ShouldReturnMultipleSlots_WhenDailyRecurrence()
    {
        // Arrange
        var start = Instant.FromUtc(2026, 5, 23, 10, 0);
        var slot = new TimeSlot(start, start.Plus(Duration.FromHours(1)));
        var recurrence = new RecurrencePatternDto("FREQ=DAILY;COUNT=3", null, 3);
        var range = new TimeSlot(start, start.Plus(Duration.FromDays(10)));

        // Act
        var result = _evaluator.ExpandOccurrences(slot, recurrence, range.Start, range.End, maxOccurrences: 100);

        // Assert
        result.Should().HaveCount(3);
        result[0].Start.Should().Be(start);
        result[1].Start.Should().Be(start.Plus(Duration.FromDays(1)));
        result[2].Start.Should().Be(start.Plus(Duration.FromDays(2)));
        // Длительность каждого слота сохраняется
        foreach (var occurrence in result)
        {
            (occurrence.End - occurrence.Start).Should().Be(Duration.FromHours(1));
        }
    }

    [Fact]
    public void ExpandOccurrences_ShouldReturnEmpty_WhenSingleSlotOutsideRange()
    {
        // Arrange — слот в прошлом, вне диапазона поиска
        var start = Instant.FromUtc(2020, 1, 1, 10, 0);
        var slot = new TimeSlot(start, start.Plus(Duration.FromHours(1)));
        var range = new TimeSlot(Instant.FromUtc(2026, 1, 1, 0, 0), Instant.FromUtc(2026, 12, 31, 0, 0));

        // Act
        var result = _evaluator.ExpandOccurrences(slot, recurrence: null, from: range.Start, to: range.End, maxOccurrences: 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExpandOccurrences_ShouldRespectMaxOccurrences()
    {
        // Arrange — бесконечное правило, ограниченное maxOccurrences
        var start = Instant.FromUtc(2026, 5, 23, 10, 0);
        var slot = new TimeSlot(start, start.Plus(Duration.FromHours(1)));
        var recurrence = new RecurrencePatternDto("FREQ=DAILY", null, null);
        var range = new TimeSlot(start, start.Plus(Duration.FromDays(1000)));

        // Act
        var result = _evaluator.ExpandOccurrences(slot, recurrence, range.Start, range.End, maxOccurrences: 5);

        // Assert
        result.Should().HaveCount(5);
    }
}
