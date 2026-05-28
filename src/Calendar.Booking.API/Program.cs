using Calendar.Booking.API.Data;
using Calendar.Booking.API.Options;
using Calendar.Booking.API.Services;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

//DB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<BookingDbContext>(option => option
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

builder.Services.AddControllers();

builder.Services.AddHealthChecks();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<BookingRabbitMqOptions>(builder.Configuration.GetSection(BookingRabbitMqOptions.SectionName));
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddEntityFrameworkOutbox<BookingDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();

        o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);

        o.QueryDelay = TimeSpan.FromSeconds(2);
        o.QueryMessageLimit = 100;
    });
    
    x.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseEntityFrameworkOutbox<BookingDbContext>(context);
    });
    
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqOptions = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<BookingRabbitMqOptions>>().Value;
        rabbitMqOptions.Validate();
        cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.VirtualHost, h =>
        {
            h.Username(rabbitMqOptions.Username);
            h.Password(rabbitMqOptions.Password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddAutoMapper(config => 
    config.AddMaps(typeof(Program)));

builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        
        options.Authority = builder.Configuration["Authentication:Authority"] ?? "http://localhost:8081/"; 
        options.Audience = builder.Configuration["Authentication:Audience"] ?? "booking_api";
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Authentication:Authority"] ?? "http://localhost:8081/", 
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Authentication:Audience"] ?? "booking_api",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ОШИБКА ВАЛИДАЦИИ ТОКЕНА]: {context.Exception.Message}\n");
                Console.ResetColor();
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[УСПЕХ]: Токен прошел проверку!\n");
                Console.ResetColor();
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                if (context.AuthenticateFailure != null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n[ПРИЧИНА ОТКАЗА]: {context.AuthenticateFailure.Message}\n");
                    Console.ResetColor();
                }
                return Task.CompletedTask;
            }
        };
    });

// Service
builder.Services.AddScoped<IBookingService, PostgresBookingService>();
builder.Services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
builder.Services.AddHostedService<IdempotencyCleanupService>();

// Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? $"localhost:{builder.Configuration["Redis:Port"] ?? "6379"}";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
});
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.Configure<DistributedLockOptions>(
    builder.Configuration.GetSection(DistributedLockOptions.SectionName));
// Time Provider
builder.Services.AddSingleton(TimeProvider.System);
//Rabbit
builder.Services.AddOptions<BookingRabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(BookingRabbitMqOptions.SectionName))
    .ValidateOnStart();

var app = builder.Build();

app.UseHealthChecks("/health");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

public partial class Program { } 