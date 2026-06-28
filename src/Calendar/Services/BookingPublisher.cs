using System.Security.Cryptography;
using System.Text;
using Calendar.Contracts.Commands;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Types;
using MassTransit;
using NodaTime;

namespace Calendar_Service.Services;

public class BookingPublisher(
    IPublishEndpoint publishEndpoint,
    IRecurrenceEvaluator recurrenceEvaluator,
    ILogger<BookingPublisher> logger) : IBookingPublisher
{
    /// <summary>Фиксированный namespace для детерминированных MessageId (RFC 4122 v5).</summary>
    private static readonly Guid MessageIdNamespace = new("a4b6c8d0-1234-4abc-9def-000000000001");

    /// <summary>Верхний предел количества вхождений, чтобы защититься от бесконечных правил.</summary>
    private const int MaxOccurrences = 365;

    /// <summary>Горизонт раскрытия повторяющихся событий от текущего момента.</summary>
    private static readonly Duration ExpansionHorizon = Duration.FromDays(365 * 2);

    public async Task PublishForAppointmentAsync(AppointmentDto dto, TimeSlot masterSlot, CancellationToken token = default)
    {
        if (dto.RoomId is not { } roomId)
        {
            return;
        }

        if (dto.Id is null || !Guid.TryParse(dto.Id, out var appointmentId))
        {
            logger.LogWarning("Не удалось опубликовать команду бронирования: некорректный AppointmentId '{Id}'.", dto.Id);
            return;
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        var to = now.Plus(ExpansionHorizon);

        // Раскрываем слоты: одно вхождение для неповторяющегося события, N — для повторяющегося.
        var slots = recurrenceEvaluator.ExpandOccurrences(masterSlot, dto.RecurrentRule, now, to, MaxOccurrences);

        if (slots.Count >= MaxOccurrences)
        {
            logger.LogWarning(
                "Событие {AppointmentId}: достигнут предел вхождений ({Max}). Правило: {Rule}. Часть вхождений не будет забронирована.",
                appointmentId, MaxOccurrences, dto.RecurrentRule?.RRule);
        }

        var title = dto.Title ?? string.Empty;

        foreach (var slot in slots)
        {
            // OccurrenceKey — дата вхождения (для повторяющегося) или null (для неповторяющегося).
            string? occurrenceKey = dto.RecurrentRule is not null
                ? slot.Start.ToDateTimeUtc().ToString("yyyy-MM-dd")
                : null;

            var messageId = DeterministicMessageId(appointmentId, occurrenceKey);

            await publishEndpoint.Publish(new BookRoomCommand(
                AppointmentId: appointmentId,
                UserId: dto.UserId,
                RoomId: roomId,
                Title: title,
                StartTimeUtc: slot.Start.ToDateTimeUtc(),
                EndTimeUtc: slot.End.ToDateTimeUtc(),
                OccurrenceKey: occurrenceKey), context =>
            {
                context.MessageId = messageId;
            }, token);
        }

        logger.LogInformation(
            "Опубликовано команд бронирования: {Count} для события {AppointmentId}, комната {RoomId}.",
            slots.Count, appointmentId, roomId);
    }

    /// <summary>
    /// Детерминированный MessageId на основе AppointmentId и OccurrenceKey,
    /// чтобы повторная доставка того же сообщения дедуплицировалась в Booking API.
    /// </summary>
    private static Guid DeterministicMessageId(Guid appointmentId, string? occurrenceKey)
    {
        var payload = occurrenceKey is null
            ? appointmentId.ToString()
            : $"{appointmentId}:{occurrenceKey}";

        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var namespaceBytes = MessageIdNamespace.ToByteArray();

        Span<byte> hashInput = stackalloc byte[namespaceBytes.Length + payloadBytes.Length];
        namespaceBytes.CopyTo(hashInput);
        payloadBytes.CopyTo(hashInput[namespaceBytes.Length..]);

        // RFC 4122 v5 (SHA-1).
        Span<byte> hash = stackalloc byte[20];
        SHA1.HashData(hashInput, hash);

        var result = hash[..16].ToArray();
        result[6] = (byte)((result[6] & 0x0F) | 0x50); // version 5
        result[8] = (byte)((result[8] & 0x3F) | 0x80); // variant

        return new Guid(result);
    }
}
