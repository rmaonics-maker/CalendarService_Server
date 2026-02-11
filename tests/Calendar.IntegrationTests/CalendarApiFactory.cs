using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Calendar_Service.Data; 
using Calendar.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Authentication;

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
        //public ITestHarness? Harness { get; private set; }
        
        public new async Task DisposeAsync()
        {
            //await Harness?.Stop();
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
                
                // Убираем основную конфигурацию MassTransit из тестовой среды
                //services.RemoveMassTransit();
               /* 
                // Используем упрощенную конфигурацию для тестов
                services.AddMassTransitTestHarness(cfg =>
                {
                    //cfg.AddConsumer<NotificationConsumer>();
                    
                    cfg.UsingInMemory((context, config) =>
                    {
                        config.ConfigureEndpoints(context);
                    });
                });
*/
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                
                
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    try
                    {
                        dbContext.Database.Migrate();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Migration Error: {ex}");
                        throw;
                    }
                
                
                //Harness = serviceProvider.GetRequiredService<ITestHarness>();
                //Harness.Start().GetAwaiter().GetResult();
                Console.WriteLine("MassTransit Test Harness initialized");
            });

            
        }
    }
}
