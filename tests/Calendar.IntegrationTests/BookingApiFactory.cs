using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Calendar.Booking.API.Data;
using Calendar.IntegrationTests.Helpers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Authentication;
using StackExchange.Redis;

namespace Calendar.IntegrationTests
{
    public class BookingApiFactory : WebApplicationFactory<BookingApiAnchor>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:15")
            .WithDatabase("BookingDb_Test")
            .WithUsername("postgres")
            .WithPassword("test_password")
            .Build();

        private readonly RedisContainer _redisContainer = new RedisBuilder()
            .Build();

        public BookingApiFactory()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        }

        public string DbConnectionString => _dbContainer.GetConnectionString();
        public string RedisConnectionString => _redisContainer.GetConnectionString();
        
        public async Task InitializeAsync()
        {
            await Task.WhenAll(_dbContainer.StartAsync(), _redisContainer.StartAsync());
            
            var optionsBuilder = new DbContextOptionsBuilder<BookingDbContext>();
            optionsBuilder.UseNpgsql(_dbContainer.GetConnectionString())
                .UseSnakeCaseNamingConvention();
            
            await using var context = new BookingDbContext(optionsBuilder.Options);
            await context.Database.MigrateAsync();
        }

        public new async Task DisposeAsync()
        {
            await Task.WhenAll(_dbContainer.StopAsync(), _redisContainer.StopAsync());
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
            builder.UseSetting("ConnectionStrings:Redis", _redisContainer.GetConnectionString());

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<BookingDbContext>>();
                services.AddDbContext<BookingDbContext>(options =>
                {
                    options.UseNpgsql(_dbContainer.GetConnectionString())
                        .UseSnakeCaseNamingConvention();
                });



                services.RemoveAll<IConnectionMultiplexer>();
                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme, options => { });

                services.AddMassTransitTestHarness();
            });
        }
    }
}
