using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Calendar_Service.Data; // <-- FIX: Твой реальный namespace
using Npgsql;
using Xunit; // <-- FIX: Обязательно для IAsyncLifetime

namespace Calendar.IntegrationTests
{
    public class CalendarApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
#pragma warning disable CS0618
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("CalendarDb_Test")
            .WithUsername("postgres")
            .WithPassword("test_password")
            .Build();
#pragma warning restore CS0618

        public new async Task DisposeAsync()
        {
            await _dbContainer.StopAsync();
        }
        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(option =>
                {
                    option.UseNpgsql(_dbContainer.GetConnectionString());
                });

                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
            });

            
        }
    }
}
