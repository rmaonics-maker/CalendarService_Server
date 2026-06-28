using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.FSharp.Core;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http;
using NBomber.Http.CSharp;

namespace Calendar.LoadTests;

class Program
{
    // === Конфигурация API ===
    const string CalendarApiUrl = "http://localhost:8080";
    const string CalendarEndpoint = "/api/v1/events";

    // === Конфигурация Booking API (где реальная синхронная проверка конфликтов комнат) ===
    const string BookingApiUrl   = "http://localhost:8082";
    const string BookingEndpoint = "/api/bookings";

    // === Конфигурация Auth (ROPC grant) ===
    const string AuthTokenUrl   = "http://localhost:8081/connect/token";
    const string ClientId       = "loadtest_client";
    const string ClientSecret   = "loadtest-secret";
    const string Username       = "admin@calendar.com";
    const string Password       = "Password123!";
    const string Scope          = "calendar_api";
    const string BookingScope   = "calendar_api booking_api"; // один токен для обоих API

    const string JsonBody = """
                            {
                              "title": "Load Test Event",
                              "description": "Created by NBomber",
                              "startTime": "2027-01-01T10:00:00Z",
                              "duration": "00:30:00",
                              "location": "Room A",
                              "recurrentRule": null
                            }
                            """;

    // Тело для burst-сценария на Booking API: ОДИНАКОВОЕ для всех 100 запросов
    // (та же комната/слот → часть 201, часть 409 Conflict).
    // Контракт CreateBookingRequest: title, roomId, startTimeUtc, endTimeUtc, appointmentId?.
    // Валидатор требует endTimeUtc - startTimeUtc >= 15 мин. roomId: 1 засеян в Booking DB.
    const string ConflictJsonBody = """
                                    {
                                      "title": "Conflict Burst Test",
                                      "roomId": 1,
                                      "startTimeUtc": "2027-06-01T10:00:00Z",
                                      "endTimeUtc": "2027-06-01T10:30:00Z",
                                      "appointmentId": null
                                    }
                                    """;
    
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
                rate: 100,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(10))
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
                        .WithBody(new StringContent(ConflictJsonBody, Encoding.UTF8, "application/json"));
                    
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
                rate: 100,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(1))
        );

        NBomberRunner
            .RegisterScenarios(thunderingHerdScenario, contentionBurstScenario)
            .WithReportFormats(ReportFormat.Html, ReportFormat.Txt)
            .WithReportFolder("results")
            .Run();
    }

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
