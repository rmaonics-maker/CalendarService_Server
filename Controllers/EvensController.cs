using AutoMapper;
using Calendar_Service.Contracts;
using Calendar_Service.Models;
using Calendar_Service.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Calendar_Service.Controllers;

[ApiController]
[Route(("api/v1/events"))]
public class EventsController(ICalendarService service, IMapper mapper, IValidator<CreateEventRequest> validator) : ControllerBase
{
    private readonly ICalendarService _service = service;
    private readonly IMapper _mapper = mapper;

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request, CancellationToken token)
    {
        // Валидацию можно добавить позже, пока верим на слово

        var updated = await _service.UpdateEventAsync(id, request, token);

        if (!updated)
        {
            return NotFound($"Event with id {id} not found");
        }

        return NoContent(); // 204 No Content - стандартный ответ на успешный Update
    }

    [HttpPost]
    //[Idempotent]
    public async Task<IActionResult> Create(CreateEventRequest request, CancellationToken token)
    {
        var validationResult = await validator.ValidateAsync(request, token);

        if (!validationResult.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        
        // Dto -> Entity
        var entity = _mapper.Map<CalendarEvent>(request);

        try
        {
            // service layer
            await _service.CreateEventAsync(entity, token); 

            return CreatedAtAction(nameof(Get), new {id = entity.Id}, _mapper.Map<EventResponse>(entity));
        }
        catch(InvalidOperationException ex)
        {
            // ПЕРЕХВАТ КОНФЛИКТА
            // Если база данных (Postgres) сказала "EXCLUDE constraint violation",
            // Сервис выбросил InvalidOperationException.
            // Мы превращаем это в HTTP 409 Conflict.
            return Conflict(new { message = ex.Message });
        }

    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var evt = _service.GetEvent(id);
        if (evt == null) return NotFound(new ProblemDetails
        {
            Title = "Event not found",
            Detail = $"Event with id {id} not exist.",
            Status = StatusCodes.Status404NotFound
        });
        
        return Ok(evt);
    }

    
    [HttpGet]
    public IActionResult GetOccurrences(
        [FromQuery] DateTimeOffset from, 
        [FromQuery] DateTimeOffset to)
    {
        // Валидация: не давай запрашивать календарь за 100 лет, сервер зависнет!
        if ((to - from).TotalDays > 365)
            return BadRequest("Range too wide. Max 1 year.");

        var events = _service.GetOccurrences(from, to);
    
       
        return Ok(events);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken token)
    {
        // Реализация контракта Delete
        // Сервис возвращает true, если удалил, false, если не нашел.
        var isDeleted = await _service.DeleteEventAsync(id, token);

        if (!isDeleted)
        {
            // Если в базе нет такого ID - 404.
            return NotFound($"Event with id {id} not found.");
        }

        // Если удалили успешно - 204 No Content.
        // Не нужно возвращать строку "Deleted", статус код говорит сам за себя.
        return NoContent();
    }

    [HttpGet("users/{userId}/history")]
    public async Task<IActionResult> GetUserHistory(Guid userId, CancellationToken token)
    {
        var history = await _service.GetUserHistoryAsync(userId, token);
        if (history == null) return NotFound("User not found.");
        return Ok(history);
    }
}