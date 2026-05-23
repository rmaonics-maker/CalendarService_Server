using System.Security.Claims;
using AutoMapper;
using Calendar_Service.Attributes;
using Calendar_Service.Models;
using Calendar_Service.Services;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Requests;
using Calendar.Contracts.Responses;
using Calendar.Contracts.Types;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using OpenIddict.Abstractions;

namespace Calendar_Service.Controllers;

[ApiController]
[Route("api/v1/events")]
public class AppointmentsController(
    IAppointmentService service, 
    IMapper mapper, 
    IValidator<CreateAppointmentRequest> validator,
    IRecurrenceEvaluator recurrenceEvaluator) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAppointment(string id, CancellationToken token = default)
    {
        var parts = id.Split('_');
        if (!Guid.TryParse(parts[0], out var masterId))
            return BadRequest("Invalid ID format.");

        var masterDto = await service.GetAppointmentAsync(masterId, token);
        if (masterDto == null) return NotFound(new ProblemDetails
        {
            Title = "Event not found",
            Detail = $"Event with id {masterId} not exist.",
            Status = StatusCodes.Status404NotFound
        });

        if (parts.Length == 1)
            return Ok(mapper.Map<AppointmentResponse>(masterDto));

        // Если это виртуальный ID, разворачиваем и ищем конкретное вхождение
        var master = mapper.Map<Appointment>(masterDto);
        var exceptions = await service.GetExceptionsAsync(masterId, token);
        
        // Для поиска одного вхождения используем широкий диапазон
        var occurrences = recurrenceEvaluator.GetOccurrences(master, exceptions, new TimeSlot(Instant.MinValue, Instant.MaxValue));
        
        var occurrence = occurrences.Events.FirstOrDefault(e => e.Id == id);
        if (occurrence == null) return NotFound(new ProblemDetails
        {
            Title = "Occurrence not found",
            Detail = $"Occurrence with id {id} not found in series.",
            Status = StatusCodes.Status404NotFound
        });

        return Ok(mapper.Map<AppointmentResponse>(occurrence));
    }

    [HttpGet]
    public async Task<IActionResult> GetAppointments(
        [FromQuery] DateTimeOffset from, 
        [FromQuery] DateTimeOffset to, CancellationToken token = default)
    {
        if ((to - from).TotalDays > 365)
            return BadRequest("Range too wide. Max 1 year.");

        var results = await service.GetAppointmentsAsync(from, to, token);
        return Ok(mapper.Map<IEnumerable<AppointmentResponse>>(results));
    }

    [HttpPost]
    [Idempotent]
    [Authorize]
    public async Task<IActionResult> CreateAppointment(CreateAppointmentRequest request, CancellationToken token = default)
    {
        var userIdClaim = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value 
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Неверный формат ID пользователя в токене.");
        
        var validationResult = await validator.ValidateAsync(request, token);
        if (!validationResult.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        
        var entity = mapper.Map<AppointmentDto>(request);
        entity = entity with { UserId = userId };

        try
        {
            var responseDto = await service.CreateAppointmentAsync(entity, token);
            return CreatedAtAction(nameof(GetAppointment), new { id = responseDto.Id }, mapper.Map<AppointmentResponse>(responseDto));
        }
        catch(InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            return Conflict(new { message = "A database error occurred while creating the event.", details = ex.InnerException?.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAppointment(Guid id, UpdateAppointmentRequest request, CancellationToken token)
    {
        var userIdClaim = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value 
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Неверный формат ID пользователя в токене.");

        var current = await service.GetAppointmentAsync(id, token);
        if (current == null) return NotFound($"Event with id {id} not found");

        if (current.UserId != userId) 
            return Forbid();
        
        var updatedDto = mapper.Map<AppointmentDto>(request);
        var result = await service.UpdateAppointmentAsync(id, updatedDto, token);
        
        if (result is null) return NotFound($"Event with id {id} not found");
        
        return CreatedAtAction(nameof(GetAppointment), new { id = result.Id }, mapper.Map<AppointmentResponse>(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAppointment(Guid id, CancellationToken token)
    {
        var isDeleted = await service.DeleteAppointmentAsync(id, token);
        if (!isDeleted) return NotFound($"Event with id {id} not found.");
        
        return NoContent();
    }
}
