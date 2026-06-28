# Calendar

Сервис календаря. Встречи, повторяющиеся события, бронирование переговорок, уведомления.
Писался как пет-проект, поэтому часть решений выглядит так, будто хотелось попробовать конкретные технологии, а не решить бизнес-задачу. Так оно и есть.

## Что внутри

Несколько сервисов на .NET 10, каждый со своей базой, общаются через RabbitMQ. Контракты живут отдельно, чтобы сервисы не ссылались друг на друга напрямую.

| Сервис | Назначение |
| --- | --- |
| `Calendar` (calendar-api) | Основной API: встречи, повторения, исключения. iCal.Net под капотом. |
| `Calendar.Auth.API` | Аутентификация, OAuth2 через OpenIddict. Выдаёт токены. |
| `Calendar.Booking.API` | Бронирование комнат. Идемпотентность, распределённые блокировки, overlap-проверки. |
| `NotificationService` | Воркер на MassTransit. Потребляет события, шлёт письма, пишет лог. |
| `Contracts` | Общие DTO, события, команды. Чистый классlib, ни от кого не зависит. |

## Стек

- .NET 10, ASP.NET Core, EF Core 10
- PostgreSQL 15 — на каждый сервис свой инстанс (да, оверкилл для пет-проекта, нет, не переделаю)
- RabbitMQ + MassTransit (EF Core outbox, дедупликация на 30 минут)
- Redis — кэш и распределённые блокировки через RedLock
- OpenIddict для OAuth2/OpenID Connect
- OpenTelemetry, Jaeger, Prometheus, Grafana — телеметрия на месте
- NodaTime (таймзоны — это боль, без неё лучше даже не начинать)

## Несколько вещей, которые стоит знать

Они неочевидны и когда-нибудь кого-нибудь сожгут. Возможно, меня.

- Корневое пространство имён `Calendar_Service`, а папка `src/Calendar`. Эту нестыковку лучше просто запомнить.
- `Booking.Period` — это `NpgsqlRange<DateTime>`, который ложится в PostgreSQL `tsrange`. Поиск пересечений — через GiST-индекс, не через обычный B-Tree. Для этого нужно расширение `btree_gist`.
- Колонки в БД — snake_case, конфигурируется через `UseSnakeCaseNamingConvention()`.
- Атрибут `[Idempotent]` кэширует ответ на 24 часа в `IMemoryCache`. Это in-memory, перезапуск сервиса всё чистит — имейте в виду при рассуждениях про гарантию.
- Секреты в Docker читаются из `/run/secrets`, если каталог существует.
- `PostgresCalendarService` управляет транзакциями вручную: `Begin/Commit/Rollback`. Никаких неявных `SaveChanges`.


## Запуск

Нужен Docker и немного удачи.

```bash
docker-compose -f Infrastructure/docker-compose.yml up --build
```

Переменные окружения лежат отдельно, в `.env` рядом с compose. В репозитории его нет, поэтому сначала надо собрать свой. Список того, что туда класть, легко выводится из `docker-compose.yml` — там всё параметризовано.

После старта:

- calendar-api — `http://localhost:8080`
- auth-api — `http://localhost:8081`
- booking-api — `http://localhost:8082`
- Jaeger UI — `http://localhost:16686`
- Grafana — `http://localhost:3000` (admin/admin)
- RabbitMQ management — `http://localhost:15672`

## Сборка и тесты

```bash
# Собрать решение
dotnet build --configuration Release

# Все тесты
dotnet test --configuration Release --verbosity normal

# Конкретный тест
dotnet test --filter "FullyQualifiedName~TestClassName"

# Один проект
dotnet test tests/Calendar.IntegrationTests/Calendar.IntegrationTests.csproj
```

Тесты трёх сортов: юнит-тесты, интеграционные и нагрузочные (NBomber). Нагрузочные запускаются отдельно и поднимают свой контур, просто так гонять их не стоит.

## Структура

```
src/
  Calendar/              — основной API
  Calendar.Auth.API/     — аутентификация
  Calendar.Booking.API/  — бронирование
  NotificationService/   — воркер уведомлений
  Contracts/             — общие контракты
tests/
  *.UnitTests/           — юнит-тесты
  *.IntegrationTests/    — интеграционные
  Calendar.LoadTests/    — нагрузочные (NBomber)
Infrastructure/
  docker-compose.yml     — локальный контур
```

## Зачем всё это

Хотелось потрогать руками MassTransit outbox, распределённые блокировки, GiST-индексы и OpenTelemetry в одном проекте. Получилось. Дальше видно будет.
