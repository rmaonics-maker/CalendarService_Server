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

        public CalendarApiFactory()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        }

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

               services.AddMassTransitTestHarness(x =>
               {
                   x.AddConsumer<NotificationConsumer>();
               });
            });
        }
    }
}
