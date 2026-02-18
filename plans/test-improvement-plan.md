# План по исправлению и дополнению тестов

## Результаты анализа тестов

### Текущее состояние

**NotificationService.Tests.dll:**
- ✅ 1 тест пройден: `Notification_Should_Be_Sent_When_Event_Created`

**Calendar.IntegrationTests.dll:**
- ❌ Нет доступных тестов - все тесты закомментированы (`//[Fact]`)
- ⚠️ Предупреждения о неиспользуемых переменных в `ChaosTests.cs`

### Обнаруженные проблемы

#### 1. Закомментированные тесты в Calendar.IntegrationTests

| Файл | Количество тестов | Статус |
|------|-------------------|--------|
| `BookingTests.cs` | 1 | Закомментирован |
| `UserTests.cs` | 1 | Закомментирован |
| `HealthCheckTests.cs` | 1 | Закомментирован |
| `ChaosTests.cs` | 3 | Закомментированы |

#### 2. MassTransit Test Harness закомментирован

В следующих файлах закомментирован код MassTransit Test Harness:
- [`BaseIntegrationTest.cs`](../tests/Calendar.IntegrationTests/BaseIntegrationTest.cs:16,32,44)
- [`CalendarApiFactory.cs`](../tests/Calendar.IntegrationTests/CalendarApiFactory.cs:23,27,55-66,84-85)

#### 3. ChaosTests.cs содержит только закомментированный код

Файл содержит неиспользуемые переменные и полностью закомментированную логику тестов.

#### 4. Отсутствующие тесты

**Unit-тесты:**
- Валидаторы: [`CreateEventRequestValidator`](../src/Calendar/Validators/CreateEventRequestValidator.cs)
- Валидаторы: [`CreateUserRequestValidator`](../src/Calendar/Validators/CreateUserRequestValidator.cs)
- Валидаторы: [`RabbitMqOptionsValidator`](../src/Calendar/Validators/RabbitMqOptionsValidator.cs)
- Контроллеры: [`EventsController`](../src/Calendar/Controllers/EventsController.cs)
- Контроллеры: [`UsersController`](../src/Calendar/Controllers/UsersController.cs)
- Сервисы: [`PostgresCalendarService`](../src/Calendar/Services/PostgresCalendarService.cs)
- Атрибуты: [`IdempotentAttribute`](../src/Calendar/Attributes/IdempotentAttribute.cs)
- Спецификации: [`BookingSpec`](../src/Calendar/Specifications/BookingSpec.cs)
- Mapping профили: [`EventMappingProfile`](../src/Calendar/Mapping/EventMappingProfile.cs)
- Mapping профили: [`UserMappingProfile`](../src/Calendar/Mapping/UserMappingProfile.cs)

**Интеграционные тесты:**
- Тесты для endpoint `/api/v1/events` (GET, PUT, DELETE)
- Тесты для endpoint `/api/v1/users/{userId}/history`
- Тесты для обработки ошибок (404, 409, 400)
- Тесты для валидации запросов
- Тесты для атрибута `[Idempotent]`

**Тесты для NotificationService:**
- Тесты для обработки ошибок потребителя
- Тесты для retry policy
- Тесты для circuit breaker

---

## План по исправлению и дополнению тестов

### Этап 1: Исправление существующих тестов

#### 1.1 Раскомментировать и исправить тесты в Calendar.IntegrationTests

**BookingTests.cs**
- [ ] Раскомментировать тест `Post_Booking_ReturnsCreatedAndResistInDb`
- [ ] Убедиться, что тест корректно проверяет создание события
- [ ] Добавить проверки для сообщения `EventCreated` в шину сообщений

**UserTests.cs**
- [ ] Раскомментировать тест `CreateUser_ShouldSaveToDatabase`
- [ ] Проверить, что тест корректно работает с текущей реализацией

**HealthCheckTests.cs**
- [ ] Раскомментировать тест `Health_ShouldReturnOk`
- [ ] Убедиться, что endpoint `/health` существует

**ChaosTests.cs**
- [ ] Реализовать тесты для сценариев chaos testing:
  - `Trigger_RetryPolicyScenario_Successfully`
  - `FatalError_GoesToErrorQueue`
  - `Success_GoesToOutbox`
- [ ] Удалить неиспользуемые переменные
- [ ] Добавить необходимые зависимости для MassTransit Test Harness

#### 1.2 Настроить MassTransit Test Harness

**BaseIntegrationTest.cs**
- [ ] Раскомментировать поле `Harness`
- [ ] Раскомментировать инициализацию в `InitializeAsync()`

**CalendarApiFactory.cs**
- [ ] Раскомментировать поле `Harness`
- [ ] Раскомментировать конфигурацию MassTransit Test Harness
- [ ] Добавить регистрацию `NotificationConsumer` для тестирования

---

### Этап 2: Создание unit-тестов

#### 2.1 Тесты валидаторов

Создать файл `tests/Calendar.UnitTests/Validators/CreateEventRequestValidatorTests.cs`:
- [ ] Тест: пустой заголовок → ошибка валидации
- [ ] Тест: заголовок длиннее 100 символов → ошибка валидации
- [ ] Тест: описание длиннее 500 символов → ошибка валидации
- [ ] Тест: пустая длительность → ошибка валидации
- [ ] Тест: отрицательная длительность → ошибка валидации
- [ ] Тест: пустое время начала → ошибка валидации
- [ ] Тест: время начала в прошлом → ошибка валидации
- [ ] Тест: пустой UserId → ошибка валидации
- [ ] Тест: валидные данные → успешная валидация

Создать файл `tests/Calendar.UnitTests/Validators/CreateUserRequestValidatorTests.cs`:
- [ ] Тест: пустое имя → ошибка валидации
- [ ] Тест: имя длиннее 50 символов → ошибка валидации
- [ ] Тест: пустая почта → ошибка валидации
- [ ] Тест: невалидный email → ошибка валидации
- [ ] Тест: валидные данные → успешная валидация

Создать файл `tests/Calendar.UnitTests/Validators/RabbitMqOptionsValidatorTests.cs`:
- [ ] Тест: пустой Host → ошибка валидации
- [ ] Тест: пустой Port → ошибка валидации
- [ ] Тест: пустой Username → ошибка валидации
- [ ] Тест: пустой Password → ошибка валидации
- [ ] Тест: валидные данные → успешная валидация

#### 2.2 Тесты сервисов

Создать файл `tests/Calendar.UnitTests/Services/PostgresCalendarServiceTests.cs`:
- [ ] Тест: `CreateEventAsync` - успешное создание события
- [ ] Тест: `CreateEventAsync` - недостаточно средств на балансе
- [ ] Тест: `CreateEventAsync` - пользователь не найден
- [ ] Тест: `CreateEventAsync` - конфликт времени (пересечение)
- [ ] Тест: `GetEvent` - событие найдено
- [ ] Тест: `GetEvent` - событие не найдено
- [ ] Тест: `GetOccurrences` - получение событий за период
- [ ] Тест: `UpdateEventAsync` - успешное обновление
- [ ] Тест: `UpdateEventAsync` - событие не найдено
- [ ] Тест: `DeleteEventAsync` - успешное удаление
- [ ] Тест: `DeleteEventAsync` - событие не найдено
- [ ] Тест: `CreateUserAsync` - успешное создание пользователя
- [ ] Тест: `CreateUserAsync` - дубликат email
- [ ] Тест: `GetUserHistoryAsync` - получение истории пользователя

#### 2.3 Тесты спецификаций

Создать файл `tests/Calendar.UnitTests/Specifications/BookingSpecTests.cs`:
- [ ] Тест: `InPeriod` - возвращает бронирования в указанном периоде
- [ ] Тест: `InPeriod` - не возвращает бронирования вне периода
- [ ] Тест: `ForRoom` - возвращает бронирования для указанной комнаты
- [ ] Тест: `ForRoom` - не возвращает бронирования для других комнат
- [ ] Тест: композиция `InPeriod` + `ForRoom`

#### 2.4 Тесты атрибутов

Создать файл `tests/Calendar.UnitTests/Attributes/IdempotentAttributeTests.cs`:
- [ ] Тест: отсутствие Idempotency-Key → BadRequest
- [ ] Тест: первый запрос с ключом → выполнение и кеширование
- [ ] Тест: повторный запрос с тем же ключом → возврат из кеша
- [ ] Тест: разные ключи → разные результаты
- [ ] Тест: кеширование на 24 часа

#### 2.5 Тесты контроллеров

Создать файл `tests/Calendar.UnitTests/Controllers/EventsControllerTests.cs`:
- [ ] Тест: `Create` - успешное создание события
- [ ] Тест: `Create` - ошибка валидации
- [ ] Тест: `Create` - конфликт времени (409)
- [ ] Тест: `Get` - событие найдено
- [ ] Тест: `Get` - событие не найдено (404)
- [ ] Тест: `GetOccurrences` - успешное получение
- [ ] Тест: `GetOccurrences` - диапазон больше года (400)
- [ ] Тест: `UpdateEvent` - успешное обновление
- [ ] Тест: `UpdateEvent` - событие не найдено (404)
- [ ] Тест: `DeleteEvent` - успешное удаление
- [ ] Тест: `DeleteEvent` - событие не найдено (404)

Создать файл `tests/Calendar.UnitTests/Controllers/UsersControllerTests.cs`:
- [ ] Тест: `Create` - успешное создание пользователя
- [ ] Тест: `Create` - ошибка валидации
- [ ] Тест: `Create` - дубликат email (409)
- [ ] Тест: `GetUserHistory` - история получена
- [ ] Тест: `GetUserHistory` - пользователь не найден (404)

---

### Этап 3: Дополнение интеграционных тестов

#### 3.1 Тесты для EventsController

Создать файл `tests/Calendar.IntegrationTests/EventsIntegrationTests.cs`:
- [ ] Тест: `GET /api/v1/events/{id}` - успешное получение события
- [ ] Тест: `GET /api/v1/events/{id}` - событие не найдено (404)
- [ ] Тест: `GET /api/v1/events?from=...&to=...` - получение событий за период
- [ ] Тест: `GET /api/v1/events?from=...&to=...` - диапазон больше года (400)
- [ ] Тест: `PUT /api/v1/events/{id}` - успешное обновление
- [ ] Тест: `PUT /api/v1/events/{id}` - событие не найдено (404)
- [ ] Тест: `DELETE /api/v1/events/{id}` - успешное удаление
- [ ] Тест: `DELETE /api/v1/events/{id}` - событие не найдено (404)
- [ ] Тест: валидация запроса создания события
- [ ] Тест: конфликт времени при создании события (409)

#### 3.2 Тесты для UsersController

Создать файл `tests/Calendar.IntegrationTests/UsersIntegrationTests.cs`:
- [ ] Тест: `POST /api/v1/users` - успешное создание пользователя
- [ ] Тест: `POST /api/v1/users` - ошибка валидации (400)
- [ ] Тест: `POST /api/v1/users` - дубликат email (409)
- [ ] Тест: `GET /api/v1/users/{userId}/history` - успешное получение истории
- [ ] Тест: `GET /api/v1/users/{userId}/history` - пользователь не найден (404)

#### 3.3 Тесты для IdempotentAttribute

Создать файл `tests/Calendar.IntegrationTests/IdempotencyTests.cs`:
- [ ] Тест: запрос без Idempotency-Key → 400 Bad Request
- [ ] Тест: первый запрос с ключом → 201 Created
- [ ] Тест: повторный запрос с тем же ключом → тот же ответ из кеша
- [ ] Тест: разные ключи → разные результаты
- [ ] Тест: кеширование работает в течение 24 часов

#### 3.4 Тесты для Outbox pattern

Создать файл `tests/Calendar.IntegrationTests/OutboxTests.cs`:
- [ ] Тест: сообщение сохраняется в Outbox таблице
- [ ] Тест: сообщение публикуется в шину
- [ ] Тест: дубликаты предотвращаются (30 минутное окно)

---

### Этап 4: Дополнение тестов для NotificationService

#### 4.1 Unit-тесты для NotificationConsumer

Создать файл `tests/NotificationService.Tests/NotificationConsumerErrorTests.cs`:
- [ ] Тест: обработка сообщения с ошибкой
- [ ] Тест: retry policy срабатывает при временной ошибке
- [ ] Тест: сообщение попадает в error queue при фатальной ошибке
- [ ] Тест: circuit breaker открывается после N ошибок

#### 4.2 Интеграционные тесты для NotificationService

Создать файл `tests/NotificationService.IntegrationTests/NotificationIntegrationTests.cs`:
- [ ] Тест: энд-ту-энд обработка сообщения
- [ ] Тест: интеграция с RabbitMQ
- [ ] Тест: обработка сообщений из error queue

---

### Этап 5: Инфраструктура тестирования

#### 5.1 Создание проекта для unit-тестов

- [ ] Создать проект `tests/Calendar.UnitTests/Calendar.UnitTests.csproj`
- [ ] Добавить необходимые пакеты:
  - `xunit`
  - `FluentAssertions`
  - `Moq` (для мокирования зависимостей)
  - `Microsoft.NET.Test.Sdk`
  - `coverlet.collector`

#### 5.2 Обновление зависимостей

- [ ] Добавить `MassTransit.Testing` в `Calendar.IntegrationTests.csproj`
- [ ] Убедиться, что все необходимые пакеты установлены

#### 5.3 Настройка CI/CD

- [ ] Настроить запуск тестов в GitHub Actions
- [ ] Добавить отчеты о покрытии кода
- [ ] Настроить уведомления о падении тестов

---

## Диаграмма архитектуры тестов

```mermaid
graph TB
    subgraph Unit Tests
        UT1[Validators Tests]
        UT2[Services Tests]
        UT3[Specifications Tests]
        UT4[Attributes Tests]
        UT5[Controllers Tests]
    end
    
    subgraph Integration Tests
        IT1[Events API Tests]
        IT2[Users API Tests]
        IT3[Booking Tests]
        IT4[Health Check Tests]
        IT5[Chaos Tests]
        IT6[Idempotency Tests]
        IT7[Outbox Tests]
    end
    
    subgraph NotificationService Tests
        NT1[Consumer Unit Tests]
        NT2[Consumer Error Tests]
        NT3[Integration Tests]
    end
    
    Unit Tests -->|Fast| CI/CD
    Integration Tests -->|Slower| CI/CD
    NotificationService Tests -->|Medium| CI/CD
```

## Приоритеты выполнения

### Высокий приоритет (P0)
1. Раскомментировать и исправить существующие тесты в `Calendar.IntegrationTests`
2. Настроить MassTransit Test Harness
3. Создать unit-тесты для валидаторов
4. Создать unit-тесты для сервисов

### Средний приоритет (P1)
5. Создать unit-тесты для контроллеров
6. Создать unit-тесты для атрибутов и спецификаций
7. Дополнить интеграционные тесты для API endpoints
8. Дополнить тесты для NotificationService

### Низкий приоритет (P2)
9. Создать интеграционные тесты для Outbox pattern
10. Настроить CI/CD и отчеты о покрытии
11. Создать тесты для chaos scenarios

## Метрики успеха

- [ ] Все закомментированные тесты раскомментированы и проходят
- [ ] Покрытие кода тестами не менее 80%
- [ ] Все unit-тесты проходят за менее чем 5 секунд
- [ ] Все интеграционные тесты проходят за менее чем 2 минуты
- [ ] Нет предупреждений при сборке тестов
- [ ] Тесты запускаются в CI/CD pipeline

---

**Дата создания:** 2026-02-18
**Статус:** Черновик
