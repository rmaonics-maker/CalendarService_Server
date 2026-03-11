namespace NotificationService.Services;

public class DummyEmailService(ILogger<DummyEmailService> logger) : IEmailService
{
    public Task SendWelcomeEmailAsync(string toEmail)
    {
        // вызов SMTP сервера здесь будет в будущем
        logger.LogInformation(">>> [EMAIL MOCK] Отправлено приветственное письмо на адрес: {Email}", toEmail);
        return Task.CompletedTask;
    }
}