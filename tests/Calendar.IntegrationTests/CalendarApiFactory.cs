using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Calendar_Service.Data;
using Calendar.IntegrationTests.Helpers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NotificationService;

namespace Calendar.IntegrationTests
{
    public class CalendarApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:15")
            .WithDatabase("CalendarDb_Test")
            .WithUsername("postgres")
            .WithPassword("test_password")
            .Build();

        public string DbConnectionString => _dbContainer.GetConnectionString();
        public ITestHarness? Harness { get; private set; }
        
        public new async Task DisposeAsync()
        {
            if (Harness != null)
            {
                await Harness.Stop();
            }
            await _dbContainer.StopAsync();
        }
        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();
            
            // Применяем миграции к тестовой базе данных
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(_dbContainer.GetConnectionString())
                .UseSnakeCaseNamingConvention();
            
            await using var context = new ApplicationDbContext(optionsBuilder.Options);
            await context.Database.MigrateAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Устанавливаем переменную окружения для отключения Health Checks в тестах
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseNpgsql(_dbContainer.GetConnectionString())
                        .UseSnakeCaseNamingConvention();
                });

               services.AddAuthentication(options =>
               {
                   options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                   options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
               })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                   TestAuthHandler.AuthenticationScheme, options => { });

               services.Configure<HealthCheckServiceOptions>(options =>
               {
                   var mtRegistration = options.Registrations.FirstOrDefault(x =>
                       x.Name == "masstransit-bus");

                   if (mtRegistration != null)
                   {
                       options.Registrations.Remove(mtRegistration);
                   }
               });
               
               services.AddMassTransitTestHarness(x =>
                {
                    x.SetKebabCaseEndpointNameFormatter();
                    
                    // Регистрация NotificationConsumer для тестирования
                    x.AddConsumer<NotificationConsumer>();
                    
                    // В тестовой среде не используем Outbox, так как он несовместим с InMemory transport
                    // Outbox предназначен для production с реальными message brokers
                    
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                    });
                });
            });
        }
    }
}
