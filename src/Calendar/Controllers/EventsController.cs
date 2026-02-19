using AutoMapper;
using Calendar_Service.Attributes;
using Calendar.Contracts;
using Calendar_Service.Models;
using Calendar_Service.Services;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Calendar_Service.Controllers;

[ApiController]
[Route(("api/v1/events"))]
public class EventsController(ICalendarService service, IMapper mapper, 
    IValidator<CreateEventRequest> validator, IPublishEndpoint publishEndpoint) : ControllerBase
{
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request, CancellationToken token)
    {
        var updated = await service.UpdateEventAsync(id, request, token);

        if (!updated)
        {
            return NotFound($"Event with id {id} not found");
        }

        return NoContent();
    }

    [HttpPost]
    [Idempotent]
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
            (Guid id, string email) = await service.CreateEventAsync(entity, publishEndpoint, token);

            return CreatedAtAction(nameof(Get), new {id = entity.Id}, mapper.Map<EventResponse>(entity));
        }
        catch(InvalidOperationException ex)
        {
            // ПЕРЕХВАТ КОНФЛИКТА
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateException ex)
        {
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
        // Валидация
        if ((to - from).TotalDays > 365)
            return BadRequest("Range too wide. Max 1 year.");

        var events = service.GetOccurrences(from, to);
    
       
        return Ok(events);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken token)
    {
        var isDeleted = await service.DeleteEventAsync(id, token);

        if (!isDeleted)
        {
            return NotFound($"Event with id {id} not found.");
        }
        
        return NoContent();
    }

    
}