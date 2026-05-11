using Calendar_Service.Consumers;
using Calendar_Service.Data;
using Calendar_Service.Options;
using Calendar_Service.Services;
using FluentValidation;
using MassTransit;
using MassTransit.Monitoring;
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
    x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();

        o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);

        o.QueryDelay = TimeSpan.FromSeconds(2);
        o.QueryMessageLimit = 100;
    });
    
    x.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
    });
    
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

    x.AddConsumer<UserRegisteredConsumer>();
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
        .AddMeter(InstrumentationOptions.MeterName)
        .AddAspNetCoreInstrumentation());

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(option => option
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IUserService, PostgresUserService>();

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
    
var app = builder.Build();


// 2. MIDDLEWARE PIPELINE (Конвейер запроса)
app.UseHealthChecks("/health");
app.UseSwagger(); 
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Calendar API V1");
});

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program { }