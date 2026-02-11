using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.IntegrationTests
{
    [CollectionDefinition(nameof(IntegrationTestCollection))]
    public class IntegrationTestCollection : ICollectionFixture<CalendarApiFactory>
    {
    }
}
