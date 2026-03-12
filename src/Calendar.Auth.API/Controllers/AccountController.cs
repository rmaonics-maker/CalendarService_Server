using Calendar.Auth.API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Calendar.Auth.API.Controllers;

[Route("[controller]")]
public class AccountController(SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet("Login")]
    public IActionResult Login(string returnUrl)
    {
        // временная генерация html для тестов
        var html = $@"
            <html><body style='font-family:sans-serif; padding: 50px; background: #1e1e1e; color: white;'>
                <h2>Вход в систему (Auth.API)</h2>
                <form method='post' action='/Account/Login'>
                    <input type='hidden' name='returnUrl' value='{returnUrl}' />
                    <p><input type='text' name='email' placeholder='Email (admin@capstore.com)' required style='padding: 10px; width: 300px;'/></p>
                    <p><input type='password' name='password' placeholder='Пароль (Password123!)' required style='padding: 10px; width: 300px;'/></p>
                    <button type='submit' style='padding: 10px 20px; cursor: pointer;'>Войти</button>
                </form>
            </body></html>";
            

        return Content(html, "text/html");
    }

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
}