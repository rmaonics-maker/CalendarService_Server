using Calendar_Service.Consumers;
using Calendar_Service.Data;
using Calendar_Service.Models;
using Calendar_Service.Options;
using Calendar_Service.Services;
using Calendar_Service.Monitoring;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. РЕГИСТРАЦИЯ СЕРВИСОВ (DI Container)

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<UserRegisteredConsumer>();
    
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
        {
            o.UsePostgres();
            o.UseBusOutbox();

            o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);

            o.QueryDelay = TimeSpan.FromSeconds(2);
            o.QueryMessageLimit = 100;
        });
    }

    if (builder.Environment.IsEnvironment("Testing"))
    {
        x.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            var rabbitMqOptions = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>().Value;
            rabbitMqOptions.Validate();
            cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.VirtualHost, h =>
            {
                h.Username(rabbitMqOptions.Username);
                h.Password(rabbitMqOptions.Password);
            });
            
            cfg.ConfigureEndpoints(context);
        });
        
        x.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            cfg.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
        });
    }
});

builder.Services.AddSwaggerGen(c => 
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Calendar API", Version = "v1" });
});

builder.Services.AddAutoMapper(config => 
    config.AddMaps(typeof(Program)));

builder.Services.AddMemoryCache();

builder.Services.AddOpenTelemetry()
    .WithTracing(providerBuilder =>
    {
        providerBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService("Calendar.Api"))
            .AddSource("MassTransit")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter()
            .AddOtlpExporter();
    })
    .WithMetrics(providerBuilder => providerBuilder
        .AddMeter(Instrumentation.MeterName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter());

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(option => option
            .UseNpgsql(connectionString, n => n.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            );

builder.Services.AddScoped<IUserService, PostgresUserService>();
builder.Services.AddScoped<IAppointmentService, PostgresAppointmentService>();
builder.Services.AddScoped<IRecurrenceEvaluator, RecurrenceEvaluator>();
builder.Services.AddScoped<IBookingPublisher, BookingPublisher>();

var secretPath = "/run/secrets";
if(Directory.Exists(secretPath))
{
    builder.Configuration.AddKeyPerFile(directoryPath: secretPath, optional: true, reloadOnChange: true);
}

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"] ?? "http://localhost:8081/"; 
        options.Audience = builder.Configuration["Authentication:Audience"] ?? "calendar_api";
        options.RequireHttpsMetadata = false; // TODO: change to true in production
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();
    
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();

    // Dev-only: создаём тестового пользователя для нагрузочных тестов
    if (app.Environment.IsDevelopment())
    {
        var loadTestUserId = Guid.Parse("71ab3b0a-a75b-4ef3-b73f-37d4e66dd2b2");
        if (!await context.Users.AnyAsync(u => u.Id == loadTestUserId))
        {
            context.Users.Add(new User
            {
                Id = loadTestUserId,
                Name = "Load Test User",
                Email = "admin@calendar.com",
                Balance = 1000.00m
            });
            await context.SaveChangesAsync();
        }
    }
}


// 2. MIDDLEWARE PIPELINE (Конвейер запроса)
app.UseHealthChecks("/health");
app.MapPrometheusScrapingEndpoint();
app.UseSwagger(); 
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Calendar API V1");
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
public class CalendarApiAnchor { } 
