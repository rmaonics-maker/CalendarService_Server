using NodaTime;

namespace Calendar_Service.Models;

public record RecurrencePattern(string RRule, Instant? Until, int? Count);