using AutoMapper;
using Calendar_Service.Models;
using Calendar_Service.Services;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Requests;
using Calendar.Contracts.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Calendar_Service.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    public class UsersController(IUserService service, IMapper mapper, IValidator<CreateUserRequest> validator) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserRequest request, CancellationToken token)
        {
            var validationResult = await validator.ValidateAsync(request, token);

            if (!validationResult.IsValid)
                return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));

            // Dto
            var userDto = mapper.Map<UserDto>(request);
            try
            {
                // service layer
                var id = await service.CreateUserAsync(userDto, token);
                return CreatedAtAction(nameof(CreateUser), id, mapper.Map<UserResponse>(userDto));
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
