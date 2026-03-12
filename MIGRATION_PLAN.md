# План миграции: Разделение Calendar и Booking API

## 🎯 Цель

Разделить ответственность между сервисами:
- **Calendar API** — управление событиями (CalendarEvent)
- **Booking API** — бронирование переговорных комнат (Booking)

---

## 🏗️ Целевая архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                    Текущая архитектура                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────┐         ┌──────────────────┐         │
│  │  Calendar API    │         │   Booking API    │         │
│  │  ┌────────────┐  │         │  ┌────────────┐ │         │
│  │  │ CalendarEvent│  │         │  │ Тестовые   │ │         │
│  │  │ Booking     │  │         │  │ данные     │ │         │
│  │  └────────────┘  │         │  └────────────┘ │         │
│  └──────────────────┘         └──────────────────┘         │
│           △                             △                    │
│           │                             │                    │
│           └──────────┬──────────────────┘                    │
│                      ▼                                     │
│                   Auth API                                  │
└─────────────────────────────────────────────────────────────┘

                        ↓ Миграция ↓

┌─────────────────────────────────────────────────────────────┐
│                    Целевая архитектура                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────┐         ┌──────────────────┐         │
│  │  Calendar API    │         │   Booking API    │         │
│  │  ┌────────────┐  │         │  ┌────────────┐ │         │
│  │  │ CalendarEvent│  │         │  │ Booking    │ │         │
│  │  │ Recurrence  │  │         │  │ Rooms      │ │         │
│  │  └────────────┘  │         │  │ Available  │ │         │
│  └────────┬─────────┘         └────────┬─────────┘         │
│           │                             │                    │
│           │ RabbitMQ (события)          │                    │
│           └──────────────┬─────────────┘                    │
│                          ▼                                  │
│                   ┌─────────────┘                           │
│                   │  Auth API   │                           │
│                   └─────────────┘                           │
└─────────────────────────────────────────────────────────────┘
```

---

## 📋 План миграции по шагам

### ✅ Шаг 0: Подготовка (сделано)
- [x] Auth API настроен и работает
- [x] Booking API создан с JWT защитой
- [x] Docker с 3 базами данных (calendar, booking, auth)

---

### 🔵 Шаг 1: Определить границы ответственности

**Calendar API:**
- Создаёт/редактирует/удаляет `CalendarEvent`
- Работает с recurrence rules (iCal.Net)
- Возвращает события для периода (GetOccurrences)
- Публикует события о создании Event в RabbitMQ

**Booking API:**
- Создаёт/отменяет `Booking` (бронь комнаты)
- Проверяет доступность комнаты (нет пересечений)
- Потребляет события из RabbitMQ
- Связывает Booking с User (через UserId из JWT)

**Вопрос для решения:**
- [ ] Что происходит при создании события с `Location = "Переговорная №1"`?
- [ ] Кто инициирует бронирование — пользователь или система?

---

### 🔵 Шаг 2: Выбрать способ коммуникации между сервисами

#### Вариант A: Синхронный (HTTP)
```
Calendar API ──HTTP POST──→ Booking API
     │                            │
     └── Создаёт Event           └── Бронирует Room
```
**Проще**, но проблема: Что если Booking API недоступен?

#### Вариант B: Асинхронный (RabbitMQ + Outbox)
```
Calendar API ──Event──→ RabbitMQ ──Consumer──→ Booking API
     │                                                │
     └── Outbox (гарантированная доставка)            └── Бронирует Room
```
**Надёжнее** (Outbox pattern из Недели 12), но eventual consistency

**Решение:** [ ] Выбрать вариант A или B

---

### 🔵 Шаг 3: Настроить Booking API для работы с реальной БД

#### 3.1. Создать модель Booking
```csharp
// Server/src/Calendar.Booking.API/Models/Booking.cs
public sealed class Booking
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public NpgsqlRange<DateTime> Period { get; set; }
    public int RoomId { get; set; }
    public Guid UserId { get; set; }
    public uint Version { get; set; }  // Оптимистичная блокировка
}

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Period)
            .HasColumnType("tsrange")
            .IsRequired();

        // GiST индекс для поиска пересечений
        builder.HasIndex(x => x.Period)
            .HasMethod("gist");

        // Оптимистичная блокировка
        builder.Property(x => x.Version)
            .IsRowVersion();
    }
}
```

#### 3.2. Создать BookingDbContext
```csharp
// Server/src/Calendar.Booking.API/Data/BookingDbContext.cs
public class BookingDbContext : DbContext
{
    public DbSet<Booking> Bookings { get; set; }

    public BookingDbContext(DbContextOptions<BookingDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new BookingConfiguration());
    }
}
```

#### 3.3. Настроить ConnectionString
```json
// Server/src/Calendar.Booking.API/appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=127.0.0.1;Port=5434;Database=booking_db;Username=postgres;Password=${DB_PASSWORD}"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

**Примечание:** Port=5434 из `docker-compose.yml` (booking-db)

#### 3.4. Зарегистрировать DbContext
```csharp
// Server/src/Calendar.Booking.API/Program.cs
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseSnakeCaseNamingConvention());
```

#### 3.5. Создать миграции
```bash
cd Server/src/Calendar.Booking.API
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**Статус:** [ ] Выполнено

---

### 🔵 Шаг 4: Реализовать BookingsController

Замени тестовые данные на реальную логику:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly BookingDbContext _context;

    [HttpGet]
    public async Task<IActionResult> GetMyBookings()
    {
        // Достаём userId из JWT токена
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
            return Unauthorized("User ID not found in token");

        var bookings = await _context.Bookings
            .Where(b => b.UserId == Guid.Parse(userId))
            .ToListAsync();

        return Ok(bookings);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        // TODO: Валидация
        // TODO: Проверка доступности комнаты
        // TODO: Создание брони
    }
}
```

**Вопросы:**
- [ ] Как проверить, что комната свободна? (SQL с пересечением диапазонов)
- [ ] Какая валидация нужна?

---

### 🔵 Шаг 5: Добавить MassTransit в Booking API

```csharp
// Program.cs
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EventCreatedConsumer>();

    x.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseEntityFrameworkOutbox<BookingDbContext>(context);
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});
```

**Статус:** [ ] Выполнено

---

### 🔵 Шаг 6: Создать Consumer для событий

```csharp
// Server/src/Calendar.Booking.API/Consumers/EventCreatedConsumer.cs
public class EventCreatedConsumer : IConsumer<EventCreated>
{
    private readonly BookingDbContext _context;
    private readonly ILogger<EventCreatedConsumer> _logger;

    public async Task Consume(ConsumeContext<EventCreated> context)
    {
        var message = context.Message;

        // TODO: Извлечь RoomId из события (если есть)
        // TODO: Проверить, нужна ли броня
        // TODO: Создать Booking
    }
}
```

**Вопрос:**
- [ ] Как понять, что событию нужна комната?
- [ ] RoomId передаётся в EventCreated или определяется по Location?

---

### 🔵 Шаг 7: Опубликовать события из Calendar API

В `CalendarService.CreateEventAsync()`:

```csharp
await publishEndpoint.Publish<EventCreated>(new
{
    EventId = entity.Id,
    Title = entity.Title,
    StartTime = entity.StartTime,
    // TODO: Добавить RoomId или Location
});
```

**Статус:** [ ] Выполнено

---

### 🔵 Шаг 8: Обработка ошибок и rollback

Что если:
- Booking API недоступен?
- Комната уже занята?
- Конфликт при бронировании?

**Решения:**
- [ ] Retry policy в MassTransit
- [ ] Compensating transaction (откат создания события)
- [ ] Eventual consistency (показывать пользователю статус "Бронирование в процессе")

---

### 🔵 Шаг 9: Защитить Calendar API (опционально)

**Вариант A:** Полностью открытый
- GET /events — публично
- POST /events — публично

**Вариант B:** Полностью защищённый
- Все endpoints требуют JWT

**Вариант C:** Hybrid
- GET /events — публично
- POST/PUT/DELETE /events — JWT

**Решение:** [ ] Выбрать вариант

---

## 🤔 Открытые вопросы

1. **Связь Event и Booking**
   - [ ] Хранить ли BookingId в CalendarEvent?
   - [ ] Или EventId в Booking?
   - [ ] Как их ассоциировать?

2. **Валидация**
   - [ ] Где проверять существование RoomId?
   - [ ] Где проверять валидность даты?

3. **Idempotency**
   - [ ] Нужно ли в Booking API?
   - [ ] Какой ключ использовать?

4. **Concurrency**
   - [ ] Два пользователя бронируют одну комнату одновременно
   - [ ] Оптимистичная блокировка через Version?

---

## 📚 Связь с теорией

- **Неделя 6** (ASP.NET Core DI): Каждый сервис — отдельное приложение
- **Неделя 8** (EF Core): Booking API работает со своей БД
- **Неделя 12** (RabbitMQ + Outbox): Надёжная коммуникация
- **Неделя 14** (Auth): Booking API защищён JWT

---

## 🚀 Чек-лист готовности

### Booking API
- [ ] BookingDbContext создан
- [ ] Миграции применены к booking-db
- [ ] BookingsController возвращает реальные данные
- [ ] MassTransit подключён
- [ ] Consumer для EventCreated

### Calendar API
- [ ] Публикует EventCreated в RabbitMQ
- [ ] Outbox pattern настроен
- [ ] ConnectionString к calendar-db

### Integration
- [ ] Тест: создаём Event → появляется Booking
- [ ] Тест: конфликт комнат (обработка)
- [ ] Тест: Booking API недоступен (retry)

---

## 📝 Заметки

**Текущий статус:** Остановился на настройке PostgreSQL для Booking Service

**Следующие шаги:**
1. Создать BookingDbContext
2. Применить миграции
3. Проверить подключение к booking-db

**Проблемы:**
- [ ] (заполнять по мере возникновения)
