using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace Calendar.LoadTests;

class Program
{
    // === Конфигурация API ===
    const string CalendarApiUrl = "http://localhost:8080";
    const string CalendarEndpoint = "/api/v1/events";

    // === Конфигурация Auth (ROPC grant) ===
    const string AuthTokenUrl   = "http://localhost:8081/connect/token";
    const string ClientId       = "loadtest_client";
    const string ClientSecret   = "loadtest-secret";
    const string Username       = "admin@calendar.com";
    const string Password       = "Password123!";
    const string Scope          = "calendar_api";

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

        var scenario = Scenario.Create("thundering_herd", async context =>
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
            var token = await FetchTokenAsync(httpClient, ctx.Logger);
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

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFormats(ReportFormat.Html, ReportFormat.Txt)
            .WithReportFolder("results")
            .Run();
    }

    // POST /connect/token с grant_type=password. Возвращает access + refresh token.
    private static async Task<TokenResponse> FetchTokenAsync(HttpClient http, Serilog.ILogger logger)
    {
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "password",
            ["client_id"]     = ClientId,
            ["client_secret"] = ClientSecret,
            ["username"]      = Username,
            ["password"]      = Password,
            ["scope"]         = Scope
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
