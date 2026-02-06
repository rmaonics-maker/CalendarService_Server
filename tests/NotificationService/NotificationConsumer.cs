using Calendar.Contracts;
using MassTransit;

namespace NotificationService;

public class NotificationConsumer(ILogger<NotificationConsumer> logger) : IConsumer<EventCreated>
{
    private readonly ILogger<NotificationConsumer> _logger = logger;

    public Task Consume(ConsumeContext<EventCreated> context)
    {
        var message = context.Message;

        #region Cimulations
            // Симуляция отправки письма
            _logger.LogInformation(
                "[EMAIL SENT] To: {Email}. Subject: '{Title}'. Event ID: {Id}. Attempt: {Attempt}",
                message.UserEmail,
                message.Title,
                message.EventId,
                context.GetRetryAttempt()
            );

            if (message.UserEmail.Contains("retry"))
            {
                if (context.GetRetryAttempt() < 3)
                {
                    _logger.LogWarning($"Имитация сбоя сети для {message.UserEmail}! Попытка {context.GetRetryAttempt()}");
                    throw new TimeoutException("имитация падения сервера");
                }
                _logger.LogInformation($"Сеть восстановилась на попытке {context.GetRetryAttempt()}. Письмо отправлено.");
            }
            else if (message.UserEmail.Contains("fatal"))
            {
                _logger.LogError($"Фатальная ошибка данных для {message.UserEmail}. Отправляем в _error очередь.");
                throw new ArgumentException("Некорректный адрес почты");
            }
            else
            {
                // Успешный сценарий
                _logger.LogInformation($"Письмо успешно отправлено на {message.UserEmail}");
            }
        #endregion
        
        return Task.CompletedTask;
    }
}
