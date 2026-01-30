using Calendar_Service.Service;
using FluentValidation;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// 1. РЕГИСТРАЦИЯ СЕРВИСОВ (DI Container)

builder.Services.AddControllers();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
c.SwaggerDoc("v1", new OpenApiInfo { Title = "Calendar API", Version = "v1" });
});

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<ICalendarService, InMemoryCalendarService>();

var app = builder.Build();


// 2. MIDDLEWARE PIPELINE (Конвейер запроса)

app.UseSwagger();
app.UseSwaggerUI(c =>
{
c.SwaggerEndpoint("/swagger/v1/swagger.json", "Calendar API V1");
});

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

using AutoMapper;
using Calendar_Service.Contracts;
using Calendar_Service.Models;

namespace Calendar_Service;

public class EventMappingProfile : Profile
{
public EventMappingProfile()
{
CreateMap<CreateEventRequest, CalendarEvent>()
.ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<CalendarEvent, EventResponse>()
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.StartTime + src.Duration));
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace Calendar_Service;

public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
private const string IdempotencyKeyHeader = "Idempotency-Key";
public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
{
if (!context.HttpContext.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var value))
{
context.Result = new BadRequestObjectResult("Idempotency key is missing");
return;
}

        var key = value.ToString();
        var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

        if (cache.TryGetValue(key, out IdempotencyRecord? idempotencyRecord))
        {
            context.Result = new ObjectResult(idempotencyRecord?.Body)
            {
                StatusCode = idempotencyRecord?.StatusCode
            };
            return;
        }

        var executedContext = await next();

        if (executedContext.Result is ObjectResult objectResult)
        {
            var record = new IdempotencyRecord(objectResult.StatusCode??200, objectResult.Value);
            cache.Set(key, record, TimeSpan.FromHours(24));
        }
    }
}

public record IdempotencyRecord(int StatusCode, object? Body);

namespace Calendar_Service.Contracts;

public sealed record CreateEventRequest(
string Title,
string Description,
DateTimeOffset StartTime,
TimeSpan Duration,
string Location,
string? RecurrentRule);

namespace Calendar_Service.Contracts;

public sealed record EventResponse(
Guid Id,
string Title,
string Description,
DateTimeOffset StartTime,
DateTimeOffset EndTime,
string Location);

using AutoMapper;
using Calendar_Service.Contracts;
using Calendar_Service.Models;
using Calendar_Service.Service;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Calendar_Service.Controllers;
[ApiController]
[Route(("api/v1/events"))]
public class EventsController(ICalendarService service, IMapper mapper, IValidator<CreateEventRequest> validator) : ControllerBase
{
private readonly ICalendarService _service = service;
private readonly IMapper _mapper = mapper;

    [HttpPost]
    [Idempotent]
    public async Task<IActionResult> Create(CreateEventRequest request)
    {
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        
        // Dto -> Entity
        var entity = _mapper.Map<CalendarEvent>(request);
        
        // service layer
        await _service.CreateEventAsync(entity);

        // Entity -> Dto response
        var response = _mapper.Map<EventResponse>(entity);
        
        // формирование ответа
        return CreatedAtAction(nameof(Get), new {id = response.Id}, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var evt = await _service.GetEventAsync(id);
        if (evt == null) return NotFound(new ProblemDetails
        {
            Title = "Event not found",
            Detail = $"Event with id {id} not exist.",
            Status = StatusCodes.Status404NotFound
        });
        
        var response = _mapper.Map<EventResponse>(evt);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (pageSize > 100) pageSize = 100;

        var events = _service.GetAllEvents()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(events);
    }
    
    [HttpGet("occurrences")]
    public IActionResult GetOccurrences(
        [FromQuery] DateTimeOffset from, 
        [FromQuery] DateTimeOffset to)
    {
        // Валидация: не давай запрашивать календарь за 100 лет, сервер зависнет!
        if ((to - from).TotalDays > 365)
            return BadRequest("Range too wide. Max 1 year.");

        var events = _service.GetOccurrences(from, to);
    
        // Маппим в Response
        var response = _mapper.Map<IEnumerable<EventResponse>>(events);
        return Ok(response);
    }
}

namespace Calendar_Service.Models;

public class CalendarEvent
{
public Guid Id { get; set; }
public string Title { get; set; } = string.Empty;
public DateTimeOffset StartTime { get; set; }
public TimeSpan Duration { get; set; }
public string Location { get; set; } = string.Empty;
public string? RecurrentRule { get; set; }
}

using System.Collections.Concurrent;
using Ical.Net.DataTypes;
using CalendarEvent = Calendar_Service.Models.CalendarEvent;

namespace Calendar_Service.Service;

public interface ICalendarService
{
Task<Guid> CreateEventAsync(CalendarEvent calendarEvent);
Task<CalendarEvent?> GetEventAsync(Guid id);
IEnumerable<CalendarEvent> GetAllEvents();
IEnumerable<CalendarEvent> GetOccurrences(DateTimeOffset from, DateTimeOffset to);
}

public class InMemoryCalendarService : ICalendarService
{
private readonly ConcurrentDictionary<Guid, CalendarEvent> _events = new();

    public Task<Guid> CreateEventAsync(CalendarEvent calendarEvent)
    {
        calendarEvent.Id = Guid.NewGuid();
        _events.TryAdd(calendarEvent.Id, calendarEvent);
        return Task.FromResult(calendarEvent.Id); // имитация асинхронности
    }

    public Task<CalendarEvent?> GetEventAsync(Guid id)
    {
        _events.TryGetValue(id, out var evt);
        return Task.FromResult(evt);
    }
    
    public IEnumerable<CalendarEvent> GetAllEvents() => _events.Values;

    public IEnumerable<CalendarEvent> GetOccurrences(DateTimeOffset from, DateTimeOffset to)
    {
        var result = new List<CalendarEvent>();

        foreach (var evt in _events.Values)
        {
            if (string.IsNullOrEmpty(evt.RecurrentRule))
            {
                if(evt.StartTime >= from && evt.StartTime <= to) result.Add(evt);
                continue;
            }
            
            try
            {
                var rrule = new RecurrencePattern(evt.RecurrentRule);
                var calendarEvent = new Ical.Net.CalendarComponents.CalendarEvent
                {
                    Start = new CalDateTime(evt.StartTime.UtcDateTime),
                    Duration = new Ical.Net.DataTypes.Duration(evt.Duration.Days, evt.Duration.Hours, evt.Duration.Minutes, evt.Duration.Seconds),
                    RecurrenceRules = new List<RecurrencePattern> { rrule }
                };
                
                var occurrences = calendarEvent.GetOccurrences(new CalDateTime(from.UtcDateTime));

                foreach (var occurrence in occurrences)
                {
                    result.Add(new CalendarEvent
                    {
                        Id = evt.Id,
                        Title = evt.Title,
                        Duration = evt.Duration,
                        Location = evt.Location,
                        RecurrentRule = evt.RecurrentRule,
                        StartTime = new DateTimeOffset(occurrence.Period.StartTime.AsUtc)
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error parsing rule for {evt.Id}: {e.Message}");
            }
        }

        return result;
    }
}

using Calendar_Service.Contracts;
using FluentValidation;

namespace Calendar_Service.Validators;

public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
public CreateEventRequestValidator()
{
RuleFor(x => x.Title)
.NotEmpty().WithMessage("Title is required.")
.MaximumLength(100).WithMessage("Title cannot be longer than 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot be longer than 500 characters.");
        
        RuleFor(x => x.Duration)
            .NotEmpty().WithMessage("Duration is required.")
            .GreaterThan(TimeSpan.Zero).WithMessage("Duration must be positive.");
        
        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required.")
            .GreaterThan(DateTimeOffset.UtcNow).WithMessage("Start time must be in the future.");
    }
}