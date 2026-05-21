using NodaTime;

namespace Calendar.Contracts.Types;

public record struct TimeSlot(Instant Start, Instant End);