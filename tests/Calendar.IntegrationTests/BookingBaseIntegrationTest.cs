using Calendar.IntegrationTests.Helpers;
using Calendar.Booking.API.Data;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Respawn;

namespace Calendar.IntegrationTests;

[Collection(nameof(BookingIntegrationTestCollection))]
public class BookingBaseIntegrationTest : IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected readonly IServiceScope Scope;
    protected readonly BookingDbContext DbContext;
    protected ITestHarness Harness;
    private readonly BookingApiFactory _factory;

    private static Respawner? _respawner;

    public BookingBaseIntegrationTest(BookingApiFactory factory)
    {
        _factory = factory;
        Client = factory.CreateClient();

        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);

        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        Harness = factory.Services.GetRequiredService<ITestHarness>();
    }

    public async Task DisposeAsync()
    {
        Scope.Dispose();
        await Task.CompletedTask;
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
}

[CollectionDefinition(nameof(BookingIntegrationTestCollection))]
public class BookingIntegrationTestCollection : ICollectionFixture<BookingApiFactory>
{
}
