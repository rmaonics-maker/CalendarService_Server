using Calendar.Api.Options;
using FluentValidation;

namespace Calendar_Service.Validators;

public class RabbitMqOptionsValidator : AbstractValidator<RabbitMqOptions>
{
    public RabbitMqOptionsValidator()
    {
        RuleFor(x => x.Host).NotEmpty().WithMessage("RabbitMQ host is required");
        RuleFor(x => x.Username).NotEmpty().WithMessage("RabbitMQ username is required");
        RuleFor(x => x.Port).Must((options, port) =>
            {
                if(options.SslEnabled) return port == 5671;
                return port == 5672;
            })
            .WithMessage("Port must be 5671 if SSL is enabled, otherwise 5672");
    }
}