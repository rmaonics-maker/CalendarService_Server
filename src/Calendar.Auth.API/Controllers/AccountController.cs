using Calendar.Auth.API.Entities;
using Calendar.Contracts.Events;
using Calendar.Contracts.Requests;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Calendar.Auth.API.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController(SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password, [FromForm] string returnUrl)
    {
        var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);
        
        if(result.Succeeded)
        {
            return Redirect(returnUrl ?? "/");
        }

        return BadRequest("Неверный логи или пароль");
    }
    
    [HttpPost("Register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, 
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] IPublishEndpoint publishEndpoint,
        [FromServices] Calendar.Auth.API.Data.AuthDbContext dbContext,
        CancellationToken cancellationToken)
    { 
// 1. Валидация логина на совпадение (проверка перед созданием)
        var existingUser = await userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
        {
            return Conflict(new { message = "Пользователь с таким логином уже существует." });
        }
        
// 2. Подготовка сущности
        var user = new ApplicationUser 
        { 
            UserName = request.Username, 
            Email = request.Email 
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
// 3. Сохранение и публикация в транзакции
            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
        
// 4. Публикация интеграционного события (попадет в Outbox того же DbContext)
            await publishEndpoint.Publish(new UserRegisteredEvent(
                Id: Guid.Parse(user.Id),
                Email: user.Email,
                Name: user.UserName,
                OccurredAt: DateTime.UtcNow
            ), cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return Ok(new { message = "Регистрация успешна" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(500, new { message = "Ошибка при регистрации", details = ex.Message });
        }
    }
}