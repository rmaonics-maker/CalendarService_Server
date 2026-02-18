# AGENTS.md

This file provides guidance to agents when working with code in this repository.

## Build & Test Commands

```bash
# Build the solution
dotnet build --configuration Release

# Run all tests
dotnet test --configuration Release --verbosity normal

# Run a specific test
dotnet test --filter "FullyQualifiedName~TestClassName"

# Run tests for a specific project
dotnet test tests/Calendar.IntegrationTests/Calendar.IntegrationTests.csproj
dotnet test tests/NotificationService.Tests/NotificationService.Tests.csproj

# Run services locally with Docker Compose
docker-compose up --build
```

## Key Non-Obvious Patterns

- **Root namespace mismatch**: Project uses `RootNamespace=Calendar_Service` but folder is `src/Calendar` - use `Calendar_Service` for namespace references
- **PostgreSQL extension required**: `btree_gist` extension must be enabled for GiST indexes (configured in ApplicationDbContext)
- **Docker secrets support**: Application reads secrets from `/run/secrets` directory if it exists (Program.cs)
- **SnakeCase naming convention**: EF Core uses `UseSnakeCaseNamingConvention()` - database columns are snake_case
- **GiST index for range queries**: Booking.Period uses PostgreSQL GiST index for tsrange overlap queries - not standard B-Tree
- **Idempotency via MemoryCache**: `[Idempotent]` attribute caches responses for 24 hours using IMemoryCache
- **Outbox pattern**: MassTransit uses EF Core outbox with 30-minute duplicate detection window
- **NpgsqlRange for time periods**: Booking.Period is `NpgsqlRange<DateTime>` mapped to PostgreSQL `tsrange` type
- **Transaction management**: PostgresCalendarService explicitly manages transactions (Begin/Commit/Rollback)
- **Specification pattern**: BookingSpec provides extension methods for query composition (InPeriod, ForRoom)
