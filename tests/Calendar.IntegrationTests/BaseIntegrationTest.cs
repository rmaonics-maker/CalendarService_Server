using Calendar.IntegrationTests.Helpers;
using Calendar_Service.Data;
using Calendar_Service.Models;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Respawn;

namespace Calendar.IntegrationTests;

[Collection("Shared Integration Collection")]
public class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected readonly IServiceScope Scope;
    protected readonly ApplicationDbContext DbContext;
    //protected readonly ITestHarness Harness;
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
        //Harness = factory.Harness;
    }

    public Task DisposeAsync()
    {
        Scope.Dispose();
        return Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("Starting MassTransit Test Harness...");
        //await Harness.Start();
        Console.WriteLine("MassTransit Test Harness started successfully");
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
