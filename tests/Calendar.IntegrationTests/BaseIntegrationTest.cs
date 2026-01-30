using Calendar_Service.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.IntegrationTests
{
    [Collection("Shared Integration Collection")]
    public class BaseIntegrationTest : IAsyncLifetime
    {
        protected readonly HttpClient Client;
        protected readonly IServiceScope Scope;
        protected readonly ApplicationDbContext DbContext;

        public BaseIntegrationTest(CalendarApiFactory factory)
        {
            Client = factory.CreateClient();
            Scope = factory.Services.CreateScope();
            DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        public Task DisposeAsync()
        {
            Scope.Dispose();
            return Task.CompletedTask;
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
