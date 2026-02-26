using AutoMapper;
using Calendar.Contracts;
using Calendar_Service.Models;
using Calendar_Service.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Calendar_Service.Controllers
{
    [ApiController]
    [Route(("api/v1/users"))]
    public class UsersController(ICalendarService service, IMapper mapper, IValidator<CreateUserRequest> validator) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken token)
        {
            var validationResult = await validator.ValidateAsync(request, token);

            if (!validationResult.IsValid)
                return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));

            // Dto -> Entity
            var entity = mapper.Map<User>(request);
            try
            {
                // service layer
                await service.CreateUserAsync(entity, token);
                return CreatedAtAction(nameof(Create), new { id = entity.Id }, mapper.Map<UserResponse>(entity));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("{userId}/history")]
        public async Task<IActionResult> GetUserHistory(Guid userId, CancellationToken token)
        {
            var history = await service.GetUserHistoryAsync(userId, token);
            if (history == null) return NotFound("User not found.");
            return Ok(history);
        }
    }
}
