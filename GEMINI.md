# Calendar Service: Инструкции по разработке

Этот проект представляет собой микросервисную систему бронирования календаря, построенную на .NET с использованием современных практик распределенных систем.

## Обзор архитектуры

- **Calendar.Api**: Основной REST API для управления событиями и пользователями.
- **NotificationService**: Фоновый воркер (Consumer), обрабатывающий события (например, отправку уведомлений).
- **Contracts**: Общие контракты (сообщения MassTransit, DTO).
- **Стек технологий**:
    - **Runtime**: .NET 8/9
    - **БД**: PostgreSQL (Entity Framework Core)
    - **Messaging**: RabbitMQ + MassTransit (с реализацией Transactional Outbox)
    - **Observability**: OpenTelemetry + Jaeger
    - **Infrastructure**: Docker & Docker Compose

## Ключевые технические решения

1.  **Работа со временем**: Используется тип `tsrange` в PostgreSQL через `NpgsqlRange<DateTime>`. При сохранении в БД `DateTime` должен иметь `DateTimeKind.Unspecified` (особенность Npgsql для диапазонов), но логически это всегда UTC.
2.  **Паттерн Outbox**: Реализован через MassTransit и Entity Framework. Сообщения сохраняются в ту же БД, что и бизнес-данные, в рамках одной транзакции, и публикуются в RabbitMQ отдельным процессом.
3.  **Безопасность транзакций**: Создание события включает проверку баланса пользователя и само бронирование в единой транзакции БД.
4.  **Идемпотентность**: Реализован атрибут `[Idempotent]`, который использует заголовок `Idempotency-Key` и кеширует ответы на 24 часа.

## Команды сборки и запуска

- **Сборка решения**: `dotnet build`
- **Запуск инфраструктуры**: `docker-compose up -d`
- **Запуск API локально**: `dotnet run --project src/Calendar/Calendar_Service.csproj`
- **Запуск тестов**: `dotnet test` (текущее состояние тестов описано в `plans/test-improvement-plan.md`).

## Конвенции разработки

- **Именование в БД**: Используется `SnakeCaseNamingConvention()` (например, `user_id` вместо `UserId`).
- **Сообщения**: Имена очередей и эндпоинтов следуют Kebab-case (MassTransit `SetKebabCaseEndpointNameFormatter`).
- **Валидация**: Все входные запросы должны валидироваться через `FluentValidation`.
- **Тестирование**:
    - Используйте `Testcontainers` для интеграционных тестов (см. `CalendarApiFactory.cs`).
    - Используйте `Respawn` для очистки БД между тестами (см. `BaseIntegrationTest.cs`).
    - **Важно**: В данный момент многие тесты закомментированы. При работе с тестами сверяйтесь с `plans/test-improvement-plan.md`.

## Инфраструктура и мониторинг

- **Jaeger UI**: `http://localhost:16686` (трассировка запросов).
- **RabbitMQ Management**: `http://localhost:15672` (логин/пароль из `.env` или `guest/guest`).
- **Health Checks**: Доступны по адресу `/health`.

## Текущие задачи (Backlog)

- [ ] Раскомментировать и исправить интеграционные тесты в `Calendar.IntegrationTests`.
- [ ] Реализовать полный набор модульных тестов согласно `plans/test-improvement-plan.md`.
- [ ] Исправить `ChaosTests.cs` для тестирования политик отказоустойчивости.
