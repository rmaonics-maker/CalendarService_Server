using System.Net;
using MassTransit;
using MassTransit.Monitoring;
using Microsoft.Extensions.Options;
using NotificationService.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NotificationService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<Worker>();

        builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
        
        builder.Services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.AddConsumer<NotificationConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                    // настройки
                var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                options.Validate();
                cfg.Host(options.Host, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                cfg.ConfigureEndpoints(context);
                
                    // политика повторов
                cfg.UseMessageRetry(r =>
                {
                    r.Exponential(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2),
                        TimeSpan.FromMilliseconds(200));
                    
                    // фильтрация
                    r.Handle<TimeoutException>();
                    r.Handle<HttpRequestException>(ex => 
                        ex.StatusCode == HttpStatusCode.ServiceUnavailable);
                    
                    r.Ignore<ArgumentException>();
                    r.Ignore<NullReferenceException>();
                });
                
                // предохранитель
                cfg.UseCircuitBreaker(cb =>
                {
                    cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                    cb.TripThreshold = 15;
                    cb.ActiveThreshold = 10;
                    cb.ResetInterval = TimeSpan.FromMinutes(5);
                });
                
                cfg.ConfigureEndpoints(context);
            });
        });

        builder.Services.AddOpenTelemetry()
            .WithTracing(providerBuilder =>
            {
                providerBuilder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService("Calendar.Worker"))
                    .AddSource("MassTransit")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter()
                    .AddOtlpExporter();
            })
            .WithMetrics(providerBuilder => providerBuilder
                .AddMeter(InstrumentationOptions.MeterName)
                .AddAspNetCoreInstrumentation());
        
        var host = builder.Build();
        host.Run();
    }
}
