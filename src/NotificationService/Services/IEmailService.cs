namespace NotificationService.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail);
}