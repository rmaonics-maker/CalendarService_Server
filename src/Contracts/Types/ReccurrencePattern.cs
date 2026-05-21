using NodaTime;

namespace Calendar.Contracts.Types;

public record RecurrencePattern(string RRule, Instant? Until, int? Count);