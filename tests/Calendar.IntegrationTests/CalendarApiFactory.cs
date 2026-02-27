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
               
               // удаление существующей конфигурации MassTransit
               services.RemoveAll<IBusControl>();
               services.RemoveAll<IBus>();
               services.RemoveAll<IBusRegistrationContext>();
               
               // и добавление туда харнеса всемогучего
               services.AddMassTransitTestHarness(x =>
               {
                   x.SetKebabCaseEndpointNameFormatter();
                   
                   // Регистрация NotificationConsumer для тестирования
                   x.AddConsumer<NotificationConsumer>();
                   
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
                   x.UsingInMemory((context, cfg) =>
                   {
                       cfg.ConfigureEndpoints(context);
                   });
               });
            });
        }
    }
}
