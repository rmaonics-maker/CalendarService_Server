using Calendar_Service.Validators;
using Calendar.Api.Options;
using FluentValidation;
using Microsoft.Extensions.Options;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidator<RabbitMqOptions>, RabbitMqOptionsValidator>();

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(options =>
            {
                var validator = new RabbitMqOptionsValidator();
                var result = validator.Validate(options);

                if(!result.IsValid)
                {
                    // логирование ошибки валидации
                    var errors = string.Join(", ", result.Errors.Select(x => x.ErrorMessage));
                    throw new OptionsValidationException(RabbitMqOptions.SectionName, typeof(RabbitMqOptions), new[] {errors});
                }
                return true;
            })
            .ValidateOnStart();

        return services;
    }
}