using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http;
using NBomber.Http.CSharp;

namespace Calendar.LoadTests;

class Program
{
    // === Конфигурация API: всё берётся из переменных окружения, дефолты — локальный docker-контур ===
    static readonly string CalendarApiUrl = Env("CALENDAR_API_URL", "http://localhost:8080");
    static readonly string CalendarEndpoint = "/api/v1/events";

    // === Конфигурация Booking API (где реальная синхронная проверка конфликтов комнат) ===
    static readonly string BookingApiUrl   = Env("BOOKING_API_URL", "http://localhost:8082");
    static readonly string BookingEndpoint = "/api/bookings";

    // === Конфигурация Auth (ROPC grant) ===
    static readonly string AuthTokenUrl   = Env("AUTH_TOKEN_URL", "http://localhost:8081/connect/token");
    static readonly string ClientId       = Env("LOADTEST_CLIENT_ID", "loadtest_client");
    static readonly string ClientSecret   = Env("LOADTEST_CLIENT_SECRET", "loadtest-secret");
    static readonly string Username       = Env("LOADTEST_USERNAME", "admin@calendar.com");
    static readonly string Password       = Env("LOADTEST_PASSWORD", "Password123!");
    static readonly string Scope          = Env("LOADTEST_SCOPE", "calendar_api");
    static readonly string BookingScope   = Env("LOADTEST_BOOKING_SCOPE", "calendar_api booking_api"); // один токен для обоих API

    // === Режимы нагрузки ===
    // default — короткие прогоны (как раньше), для быстрой проверки.
    // demo — ~2 минуты устойчивой записи + периодические burst'ы за один слот,
    //        чтобы на Grafana-графиках (scrape 15s) было видно и ровную нагрузку, и всплески 409.
    static readonly string Mode = Env("LOADTEST_MODE", "default");

    // default-режим
    static readonly int HerdRate    = EnvInt("LOADTEST_HERD_RATE", 100);
    static readonly int HerdSeconds = EnvInt("LOADTEST_HERD_SECONDS", 10);
    static readonly int BurstSize   = EnvInt("LOADTEST_BURST_SIZE", 100);

    // demo-режим
    static readonly int DemoRate       = EnvInt("LOADTEST_DEMO_RATE", 50);
    static readonly int DemoSeconds    = EnvInt("LOADTEST_DEMO_SECONDS", 110);
    static readonly int DemoBurstCount = EnvInt("LOADTEST_DEMO_BURSTS", 5);
    static readonly int DemoBurstEvery = EnvInt("LOADTEST_DEMO_BURST_EVERY", 20); // секунды между burst'ами

    // Слот для contention-burst'ов: уникальный на каждый запуск процесса (случайный
    // внутри года вперёд). Гарантирует воспроизводимый расклад «1×201 + 99×409»
    // на первом burst'е каждой серии, а не только при самом первом прогоне в жизни базы.
    static readonly DateTime ConflictSlot =
        DateTime.UtcNow.AddYears(1).AddMinutes(Random.Shared.Next(0, 365 * 24 * 2) * 30);

    private static string BuildRequestBody(long invocationCounter)
    {
        // Уникальный слот: базовая дата + сдвиг по 45 минут на каждый запрос.
        var baseStart = new DateTime(2027, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var slotStart = baseStart.AddMinutes(45 * invocationCounter);
        var body = $$"""
                     {
                       "title": "Load Test Event",
                       "description": "Created by NBomber",
                       "startTime": "{{slotStart:yyyy-MM-ddTHH:mm:ssZ}}",
                       "duration": "00:30:00",
                       "location": "Room A",
                       "recurrentRule": null,
                       "roomId": 1
                     }
                     """;
        return body;
    }

    // Тело burst-сценариев на Booking API: ОДИНАКОВОЕ для всех запросов серии
    // (та же комната/слот → часть 201, часть 409 Conflict).
    // Контракт CreateBookingRequest: title, roomId, startTimeUtc, endTimeUtc, appointmentId?.
    // Валидатор требует endTimeUtc - startTimeUtc >= 15 мин. roomId: 1 засеян в Booking DB.
    private static string BuildConflictBody() => $$"""
                                                 {
                                                   "title": "Conflict Burst Test",
                                                   "roomId": 1,
                                                   "startTimeUtc": "{{ConflictSlot:yyyy-MM-ddTHH:mm:ssZ}}",
                                                   "endTimeUtc": "{{ConflictSlot.AddMinutes(30):yyyy-MM-ddTHH:mm:ssZ}}",
                                                   "appointmentId": null
                                                 }
                                                 """;

    static async Task Main(string[] args)
    {
        using var httpClient = Http.CreateDefaultClient();

        // === 1. Получение JWT через ROPC grant один раз перед стартом сценария ===
        string accessToken = string.Empty;
        string bookingAccessToken = string.Empty;

        var thunderingHerdScenario = Scenario.Create("thundering_herd", async context =>
        {
            var createStep = await Step.Run<HttpResponseMessage>(
                "create_appointment",
                context,
                async () =>
                {
                    var body = BuildRequestBody(context.InvocationNumber);
                    var request = Http.CreateRequest("POST", CalendarApiUrl + CalendarEndpoint)
                        .WithHeader("Authorization", $"Bearer {accessToken}")
                        .WithHeader("Idempotency-Key", Guid.NewGuid().ToString())
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(new StringContent(body, Encoding.UTF8, "application/json"));

                    var response = await Http.Send(httpClient, request);

                    return response;
                });

            return Response.Ok();
        })
        .WithInit(async ctx =>
        {
            var token = await FetchTokenAsync(httpClient, ctx.Logger, Scope);
            accessToken = token.AccessToken;
            ctx.Logger.Information("Load test JWT acquired, expires in {ExpiresIn}s", token.ExpiresIn);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: HerdRate,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(HerdSeconds))
        );

        // === Сценарий contention_burst: 100 запросов максимально одновременно ===
        // ОДИНАКОВОЕ тело (та же комната/слот), РАЗНЫЕ Idempotency-Key.
        // Ожидаемо: 1×201 (победитель Redis-блокировки) и 99×409 Conflict.
        // LoadSimulation: Inject(rate=100, interval=1s, during=1s) → ровно один «взрыв»,
        // все 100 запросов уходят за один тик планировщика.
        // Warmup отключён: при [Idempotent] первый warmup-запрос занял бы слот и сбил гонку.
        var contentionBurstScenario = Scenario.Create("contention_burst", async context =>
        {
            await Step.Run(
                "create_booking",
                context,
                async () =>
                {
                    var request = Http.CreateRequest("POST", BookingApiUrl + BookingEndpoint)
                        .WithHeader("Authorization", $"Bearer {bookingAccessToken}")
                        .WithHeader("Idempotency-Key", Guid.NewGuid().ToString())
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(new StringContent(BuildConflictBody(), Encoding.UTF8, "application/json"));

                    var nbomberResponse = await Http.Send(httpClient, request);

                    return nbomberResponse;
                });

            return Response.Ok();
        })
        .WithInit(async ctx =>
        {
            var token = await FetchTokenAsync(httpClient, ctx.Logger, BookingScope);
            bookingAccessToken = token.AccessToken;
            ctx.Logger.Information("Burst test JWT acquired (booking_api), expires in {ExpiresIn}s", token.ExpiresIn);
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(
                rate: BurstSize,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(1))
        );

        // === Демо-сценарии (LOADTEST_MODE=demo): заточены под живой показ в Grafana ===

        // Ровная запись в calendar-api: каждый POST гоняет всю цепочку
        // Postgres → MassTransit/RabbitMQ → BookRoomCommand → Redis RedLock → booking → уведомление.
        var steadyLoadScenario = Scenario.Create("steady_load", async context =>
        {
            await Step.Run<HttpResponseMessage>(
                "create_appointment",
                context,
                async () =>
                {
                    var body = BuildRequestBody(context.InvocationNumber);
                    var request = Http.CreateRequest("POST", CalendarApiUrl + CalendarEndpoint)
                        .WithHeader("Authorization", $"Bearer {accessToken}")
                        .WithHeader("Idempotency-Key", Guid.NewGuid().ToString())
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(new StringContent(body, Encoding.UTF8, "application/json"));

                    return await Http.Send(httpClient, request);
                });

            return Response.Ok();
        })
        .WithInit(async ctx =>
        {
            var token = await FetchTokenAsync(httpClient, ctx.Logger, Scope);
            accessToken = token.AccessToken;
            ctx.Logger.Information("Demo JWT acquired, expires in {ExpiresIn}s", token.ExpiresIn);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: DemoRate,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(DemoSeconds))
        );

        // Серия burst'ов за один и тот же слот: первый burst — 1×201 + (N-1)×409,
        // остальные — сплошные 409 (слот уже занят). На дашборде это пилообразные
        // всплески конфликтов и блокировок поверх ровного RPS.
        var bursts = new List<LoadSimulation>();
        for (var i = 0; i < DemoBurstCount; i++)
        {
            bursts.Add(Simulation.Inject(
                rate: BurstSize,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(1)));
            if (i < DemoBurstCount - 1)
                bursts.Add(Simulation.Pause(TimeSpan.FromSeconds(DemoBurstEvery - 1)));
        }

        var contentionBurstsScenario = Scenario.Create("contention_bursts", async context =>
        {
            await Step.Run(
                "create_booking",
                context,
                async () =>
                {
                    var request = Http.CreateRequest("POST", BookingApiUrl + BookingEndpoint)
                        .WithHeader("Authorization", $"Bearer {bookingAccessToken}")
                        .WithHeader("Idempotency-Key", Guid.NewGuid().ToString())
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(new StringContent(BuildConflictBody(), Encoding.UTF8, "application/json"));

                    return await Http.Send(httpClient, request);
                });

            return Response.Ok();
        })
        .WithInit(async ctx =>
        {
            var token = await FetchTokenAsync(httpClient, ctx.Logger, BookingScope);
            bookingAccessToken = token.AccessToken;
            ctx.Logger.Information("Demo burst JWT acquired (booking_api), expires in {ExpiresIn}s", token.ExpiresIn);
        })
        .WithoutWarmUp()
        .WithLoadSimulations(bursts.ToArray());

        if (string.Equals(Mode, "demo", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Demo mode: steady {DemoRate} rps x {DemoSeconds}s + {DemoBurstCount} bursts x {BurstSize} reqs");
            NBomberRunner
                .RegisterScenarios(steadyLoadScenario, contentionBurstsScenario)
                .WithReportFormats(ReportFormat.Html, ReportFormat.Txt)
                .WithReportFolder("results")
                .Run();
        }
        else
        {
            NBomberRunner
                .RegisterScenarios(thunderingHerdScenario, contentionBurstScenario)
                .WithReportFormats(ReportFormat.Html, ReportFormat.Txt)
                .WithReportFolder("results")
                .Run();
        }
    }

    private static string Env(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

    private static int EnvInt(string name, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value > 0 ? value : fallback;

    // POST /connect/token с grant_type=password. Возвращает access + refresh token.
    private static async Task<TokenResponse> FetchTokenAsync(HttpClient http, Serilog.ILogger logger, string scope)
    {
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "password",
            ["client_id"]     = ClientId,
            ["client_secret"] = ClientSecret,
            ["username"]      = Username,
            ["password"]      = Password,
            ["scope"]         = scope
        });

        using var resp = await http.PostAsync(AuthTokenUrl, form);
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            logger.Error("Token endpoint failed: {Status} {Body}", resp.StatusCode, body);
            throw new InvalidOperationException($"Не удалось получить JWT: {resp.StatusCode} {body}");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var token = JsonSerializer.Deserialize<TokenResponse>(body, options)
                    ?? throw new InvalidOperationException("Пустой ответ от token endpoint.");
        return token;
    }

    // Минимальная модель ответа /connect/token
    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
