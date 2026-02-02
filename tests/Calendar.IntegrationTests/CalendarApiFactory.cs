using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Calendar_Service.Data; 
using Npgsql;
using Xunit;
using Calendar.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Authentication;

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

        public string DbConnectionString => _dbContainer.GetConnectionString();


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
                services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseNpgsql(_dbContainer.GetConnectionString());
                });

               services.AddAuthentication(options =>
               {
                   options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                   options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
               })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                   TestAuthHandler.AuthenticationScheme, options => { });

                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
            });

            
        }
    }
}
