using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.IntegrationTests
{
    [CollectionDefinition("Shared Integration Collection")]
    public class IntegrationTestCollection : ICollectionFixture<CalendarApiFactory>
    {
    }
}
