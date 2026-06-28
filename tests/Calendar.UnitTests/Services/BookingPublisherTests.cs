using Calendar.Contracts.Commands;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Types;
using Calendar_Service.Services;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using NodaTime;
using NSubstitute;
using NSubstitute.ReceivedExtensions;

namespace Calendar.UnitTests.Services;

public class BookingPublisherTests
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IRecurrenceEvaluator _recurrenceEvaluator;
    private readonly BookingPublisher _publisher;
    private readonly List<object> _captured;

    public BookingPublisherTests()
    {
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _recurrenceEvaluator = Substitute.For<IRecurrenceEvaluator>();
        var logger = Substitute.For<ILogger<BookingPublisher>>();
        _publisher = new BookingPublisher(_publishEndpoint, _recurrenceEvaluator, logger);
        _captured = new List<object>();

        // IPublishEndpoint.Publish<T>(T message, IPipe<T> pipe, CancellationToken) —
        // это низкоуровневый метод, который вызывают методы расширения PublishExecuteExtensions.
        // Перехватываем его и сохраняем все опубликованные сообщения.
        _publishEndpoint
            .WhenForAnyArgs(p => p.Publish(default(BookRoomCommand)!, default(IPipe<PublishContext<BookRoomCommand>>)!))
            .Do(call =>
            {
                if (call[0] is BookRoomCommand cmd)
                    _captured.Add(cmd);
            });
    }

    private static AppointmentDto Dto(Guid id, int? roomId, RecurrencePatternDto? recurrence) => new()
    {
        Id = id.ToString(),
        Title = "Test Event",
        UserId = Guid.NewGuid(),
        StartTime = DateTimeOffset.UtcNow.AddDays(1),
        Duration = TimeSpan.FromHours(1),
        RoomId = roomId,
        RecurrentRule = recurrence
    };

    private IReadOnlyList<BookRoomCommand> Commands => _captured.OfType<BookRoomCommand>().ToList();

    [Fact]
    public async Task PublishForAppointmentAsync_ShouldNotPublish_WhenRoomIdIsNull()
    {
        // Arrange — событие без комнаты
        var dto = Dto(Guid.NewGuid(), roomId: null, recurrence: null);

        // Act
        await _publisher.PublishForAppointmentAsync(dto, new TimeSlot(SystemClock.Instance.GetCurrentInstant(), SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromHours(1))));

        // Assert — команда не публикуется
        Commands.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishForAppointmentAsync_ShouldPublishOneCommand_WhenNonRecurring()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var dto = Dto(appointmentId, roomId: 1, recurrence: null);
        var start = Instant.FromUtc(2027, 1, 1, 10, 0);
        var masterSlot = new TimeSlot(start, start.Plus(Duration.FromHours(1)));

        _recurrenceEvaluator.ExpandOccurrences(Arg.Any<TimeSlot>(), Arg.Any<RecurrencePatternDto?>(), Arg.Any<Instant>(), Arg.Any<Instant>(), Arg.Any<int>())
            .Returns(new List<TimeSlot> { masterSlot });

        // Act
        await _publisher.PublishForAppointmentAsync(dto, masterSlot);

        // Assert — ровно одна команда
        Commands.Should().HaveCount(1);
        Commands[0].AppointmentId.Should().Be(appointmentId);
        Commands[0].RoomId.Should().Be(1);
    }

    [Fact]
    public async Task PublishForAppointmentAsync_ShouldPublishMultipleCommands_WhenRecurring()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var dto = Dto(appointmentId, roomId: 2,
            recurrence: new RecurrencePatternDto("FREQ=DAILY;COUNT=3", null, 3));
        var start = Instant.FromUtc(2027, 1, 1, 10, 0);
        var masterSlot = new TimeSlot(start, start.Plus(Duration.FromHours(1)));

        // Имитируем раскрытие в 3 слота
        var slots = new List<TimeSlot>
        {
            masterSlot,
            new(start.Plus(Duration.FromDays(1)), start.Plus(Duration.FromDays(1)).Plus(Duration.FromHours(1))),
            new(start.Plus(Duration.FromDays(2)), start.Plus(Duration.FromDays(2)).Plus(Duration.FromHours(1)))
        };
        _recurrenceEvaluator.ExpandOccurrences(Arg.Any<TimeSlot>(), Arg.Any<RecurrencePatternDto?>(), Arg.Any<Instant>(), Arg.Any<Instant>(), Arg.Any<int>())
            .Returns(slots);

        // Act
        await _publisher.PublishForAppointmentAsync(dto, masterSlot);

        // Assert — три команды с той же комнатой и appointmentId
        Commands.Should().HaveCount(3);
        Commands.All(c => c.RoomId == 2).Should().BeTrue();
        Commands.All(c => c.AppointmentId == appointmentId).Should().BeTrue();
        // Вхождения идут в порядке возрастания времени
        Commands[1].StartTimeUtc.Should().Be(start.Plus(Duration.FromDays(1)).ToDateTimeUtc());
        Commands[2].StartTimeUtc.Should().Be(start.Plus(Duration.FromDays(2)).ToDateTimeUtc());
    }

    [Fact]
    public async Task PublishForAppointmentAsync_ShouldPassCorrectFields()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = new AppointmentDto
        {
            Id = appointmentId.ToString(),
            Title = "Standup",
            UserId = userId,
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            Duration = TimeSpan.FromMinutes(30),
            RoomId = 3
        };
        var start = Instant.FromUtc(2027, 1, 1, 10, 0);
        var masterSlot = new TimeSlot(start, start.Plus(Duration.FromMinutes(30)));

        _recurrenceEvaluator.ExpandOccurrences(Arg.Any<TimeSlot>(), Arg.Any<RecurrencePatternDto?>(), Arg.Any<Instant>(), Arg.Any<Instant>(), Arg.Any<int>())
            .Returns(new List<TimeSlot> { masterSlot });

        // Act
        await _publisher.PublishForAppointmentAsync(dto, masterSlot);

        // Assert
        Commands.Should().HaveCount(1);
        var cmd = Commands[0];
        cmd.RoomId.Should().Be(3);
        cmd.Title.Should().Be("Standup");
        cmd.UserId.Should().Be(userId);
        cmd.AppointmentId.Should().Be(appointmentId);
        cmd.StartTimeUtc.Should().Be(start.ToDateTimeUtc());
        cmd.EndTimeUtc.Should().Be(start.Plus(Duration.FromMinutes(30)).ToDateTimeUtc());
        // OccurrenceKey = null для неповторяющегося события
        cmd.OccurrenceKey.Should().BeNull();
    }
}

