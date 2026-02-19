using AutoMapper;
using Calendar.Contracts;
using Calendar_Service.Models;
using Calendar_Service.Services;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Calendar_Service.Controllers;

[ApiController]
[Route(("api/v1/events"))]
public class EventsController(ICalendarService service, IMapper mapper, IValidator<CreateEventRequest> validator, IPublishEndpoint publishEndpoint) : ControllerBase
{
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request, CancellationToken token)
    {
        var updated = await service.UpdateEventAsync(id, request, token);

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
        var entity = mapper.Map<CalendarEvent>(request);

        try
        {
            // service layer
            (Guid id, string email) = await service.CreateEventAsync(entity, token);

            await publishEndpoint.Publish(new EventCreated
            {
                EventId = id,
                Title = request.Title,
                UserId = request.UserId,
                UserEmail = email,       
                OccurredAt = DateTime.UtcNow
            }, token);

            return CreatedAtAction(nameof(Get), new {id = entity.Id}, mapper.Map<EventResponse>(entity));
        }
        catch(InvalidOperationException ex)
        {
            // ПЕРЕХВАТ КОНФЛИКТА
            // Если база данных (Postgres) сказала "EXCLUDE constraint violation",
            // Сервис выбросил InvalidOperationException.
            // Мы превращаем это в HTTP 409 Conflict.
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            // Это может произойти, если есть конфликт в базе данных, например, дубликат ключа
            return Conflict(new { message = "A database error occurred while creating the event.", details = ex.InnerException?.Message });
        }

    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var evt = service.GetEvent(id);
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

        var events = service.GetOccurrences(from, to);
    
       
        return Ok(events);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken token)
    {
        // Реализация контракта Delete
        // Сервис возвращает true, если удалил, false, если не нашел.
        var isDeleted = await service.DeleteEventAsync(id, token);

        if (!isDeleted)
        {
            // Если в базе нет такого ID - 404.
            return NotFound($"Event with id {id} not found.");
        }

        // Если удалили успешно - 204 No Content.
        // Не нужно возвращать строку "Deleted", статус код говорит сам за себя.
        return NoContent();
    }

    
}