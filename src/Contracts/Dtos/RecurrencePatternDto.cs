using NodaTime;

namespace Calendar.Contracts.Dtos;

public record RecurrencePatternDto(string RRule, Instant? Until, int? Count);