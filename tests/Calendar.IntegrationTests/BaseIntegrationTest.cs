using Calendar.IntegrationTests.Helpers;
using Calendar_Service.Data;
using Calendar_Service.Models;
using Microsoft.Extensions.DependencyInjection;
using Respawn;

namespace Calendar.IntegrationTests;

[Collection("Shared Integration Collection")]
public class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected readonly IServiceScope Scope;
    protected readonly ApplicationDbContext DbContext;
    private readonly CalendarApiFactory _factory;

    private static Respawner? _respawner;


    public BaseIntegrationTest(CalendarApiFactory factory)
    {
        _factory = factory;
        Client = factory.CreateClient();

        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);

        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public Task DisposeAsync()
    {
        Scope.Dispose();
        return Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        await using var connection = new Npgsql.NpgsqlConnection(_factory.DbConnectionString);
        await connection.OpenAsync();

        _respawner ??= await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = ["__EFMigrationsHistory"],
            SchemasToInclude = ["public"],
            DbAdapter = DbAdapter.Postgres
        });

        await _respawner.ResetAsync(connection);
    }

    protected async Task<User> CreateUserInDbAsync()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), 
            Name = "Test Booker",
            Email = $"booker_{Guid.NewGuid()}@example.com",
            Balance = 1000 
        };

        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        return user;
    }
}
