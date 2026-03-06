using System.Security.Cryptography.X509Certificates;

namespace Calendar.Contracts;

public record EventResponse(
    Guid Id,
    string Title,
    string Location,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime)
{
    // Конструктор без параметров для AutoMapper
    public EventResponse() : this(default, string.Empty, string.Empty, default, default) { }
};
