# Calendar Service Project Context

## Project Overview
Проект представляет собой распределенную систему управления событиями календаря на платформе .NET 10. Система состоит из основного API и фонового воркера для обработки уведомлений.

- **Main API (`src/Calendar`)**: REST API для создания, обновления и получения событий календаря, пользователей и бронирований.
- **Notification Service (`tests/NotificationService`)**: Фоновый сервис (воркер), потребляющий сообщения из RabbitMQ для обработки уведомлений.
- **Contracts (`src/Contracts`)**: Общая библиотека DTO и сообщений для взаимодействия между сервисами.

## Tech Stack
- **Framework**: .NET 10.0
- **Database**: PostgreSQL (Entity Framework Core с использованием `EFCore.NamingConventions` для Snake Case).
- **Messaging**: RabbitMQ через MassTransit.
- **Mapping**: AutoMapper.
- **Validation**: FluentValidation.
- **Documentation**: Swagger/OpenAPI.
- **Testing**: xUnit, Testcontainers (для PostgreSQL), Respawn (для очистки БД в тестах).

## Architecture & Design Patterns
- **Database Conventions**: В базе данных используется `snake_case` для имен таблиц и колонок.
- **Service Layer**: Бизнес-логика вынесена в сервисы (например, `PostgresCalendarService`).
- **Asynchronous Communication**: При создании событий публикуется сообщение `EventCreated`, которое обрабатывается `NotificationConsumer`.
- **Integration Testing**: Интеграционные тесты используют реальный PostgreSQL в контейнере и автоматически сбрасывают состояние БД между тестами.

## Key Files & Directories
- `src/Calendar/Program.cs`: Конфигурация DI, MassTransit, EF Core и Middleware API.
- `tests/NotificationService/Program.cs`: Конфигурация воркера и потребителей (Consumers).
- `src/Calendar/Data/ApplicationDbContext.cs`: Контекст базы данных.
- `docker-compose.yml`: Определение инфраструктуры (PostgreSQL, RabbitMQ, API и Worker).
- `tests/Calendar.IntegrationTests/NotificationTests.cs`: Тесты потребителя уведомлений (MassTransit TestHarness), проверяющие логику ретраев и обработки ошибок.
- `tests/Calendar.IntegrationTests/`: Набор интеграционных тестов.

## Building and Running

### Prerequisites
- .NET 10 SDK
- Docker & Docker Compose

### Commands
- **Infrastructure**: `docker-compose up -d` (запуск Postgres и RabbitMQ).
- **Build**: `dotnet build`
- **Run API**: `dotnet run --project src/Calendar/Calendar_Service.csproj`
- **Run Worker**: `dotnet run --project tests/NotificationService/NotificationService.csproj`
- **Tests**: `dotnet test`
- **Migrations**: 
  - Создание: `dotnet ef migrations add <Name> --project src/Calendar`
  - Применение: `dotnet ef database update --project src/Calendar`

## Development Conventions
- Всегда использовать асинхронные методы (`Task`, `await`) для I/O операций.
- При изменении API обновлять контракты в `src/Contracts`.
- Новые эндпоинты должны сопровождаться валидацией через `FluentValidation`.
- При добавлении функционала, требующего фоновой обработки, использовать MassTransit для публикации событий.
