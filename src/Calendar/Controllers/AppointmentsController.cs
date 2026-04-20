using System.Security.Claims;
using AutoMapper;
using Calendar_Service.Attributes;
using Calendar_Service.Services;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Requests;
using Calendar.Contracts.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Calendar_Service.Controllers;

[ApiController]
[Route(("api/v1/events"))]
public class AppointmentsController(IAppointmentService service, IMapper mapper, 
    IValidator<CreateAppointmentRequest> validator) : ControllerBase
{
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAppointment(Guid id, UpdateAppointmentRequest request, CancellationToken token)
    {
        // UserID из клейма
        var userIdClaim = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value 
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Неверный формат ID пользователя в токене.");
        }
        // TODO сделать валидацию UpdateAppointmentRequest
        // проверка существования события и принадлежности пользователю.
        var appointmentDto = await service.GetAppointmentAsync(id, token);
        if (appointmentDto is null)
        {
            return NotFound($"Event with id {id} not found");
        }
// TODO переделать Forbid
        if (appointmentDto.UserId != userId) 
            return Forbid("У вас нет прав для редактирования этого события.");
        
        // обновление события
        var updatedAppointmentDto = mapper.Map<AppointmentDto>(request);
        var success = await service.UpdateAppointmentAsync(id, updatedAppointmentDto, token);
        
        if(success is null)
            return NotFound($"Event with id {id} not found");
        
        return CreatedAtAction(nameof(GetAppointment), new {id = success.Id}, mapper.Map<AppointmentResponse>(success));
    }

    [HttpPost]
    [Idempotent]
    [Authorize]
    public async Task<IActionResult> CreateAppointment(CreateAppointmentRequest request, CancellationToken token = default)
    {
        // UserID из клейма
        var userIdClaim = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value 
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Неверный формат ID пользователя в токене.");
        }
        
        //  Валидация
        var validationResult = await validator.ValidateAsync(request, token);
        if (!validationResult.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        
        // Dto -> Entity
        var entity = mapper.Map<AppointmentDto>(request);
        entity = entity with {UserId = userId};

        try
        {
            var responseDto = await service.CreateAppointmentAsync(entity, token);

            return CreatedAtAction(nameof(GetAppointment), new {id = responseDto.Id}, mapper.Map<AppointmentResponse>(entity));
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
    public async Task<IActionResult> GetAppointment(Guid id, CancellationToken token = default)
    {
        var evt = await service.GetAppointmentAsync(id, token);
        if (evt == null) return NotFound(new ProblemDetails
        {
            Title = "Event not found",
            Detail = $"Event with id {id} not exist.",
            Status = StatusCodes.Status404NotFound
        });
        
        return Ok(mapper.Map<AppointmentResponse>(evt));
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAppointments(
        [FromQuery] DateTimeOffset from, 
        [FromQuery] DateTimeOffset to, CancellationToken token = default)
    {
        // Валидация
        if ((to - from).TotalDays > 365)
            return BadRequest("Range too wide. Max 1 year.");

        var events = await service.GetAppointmentsAsync(from, to, token);
        
        return Ok(mapper.Map<IEnumerable<AppointmentResponse>>(events));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAppointment(Guid id, CancellationToken token)
    {
        var isDeleted = await service.DeleteAppointmentAsync(id, token);

        if (!isDeleted)
        {
            return NotFound($"Event with id {id} not found.");
        }
        
        return NoContent();
    }
}