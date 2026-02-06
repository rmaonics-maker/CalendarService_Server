using System.Net;
using System.Net.Http.Json; 
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Calendar.IntegrationTests;

public class UserTests(CalendarApiFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateUser_ShouldSaveToDatabase()
    {
        // 1. Arrange
        // Генерируем уникальные данные, чтобы тесты не конфликтовали
        var uniqueEmail = $"test_user_{Guid.NewGuid()}@example.com";
        var newUser = new
        {
            Name = "Integration Tester",
            Email = uniqueEmail
        };

        // 2. Act
        // Отправляем POST запрос в API. 
        var response = await Client.PostAsJsonAsync("/api/v1/users", newUser);

        // 3. Assert (HTTP Level)
        // Проверяем, что API не упал и вернул успех
        response.EnsureSuccessStatusCode(); // Выкинет исключение, если не 2xx
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        // 4. Assert (Database Level)
        // Используем Scope, созданный в BaseIntegrationTest
        var userInDb = await DbContext.Users
            .FirstOrDefaultAsync(u => u.Email == uniqueEmail);

        userInDb.Should().NotBeNull("пользователь должен был сохраниться в БД");
        userInDb!.Name.Should().Be("Integration Tester");
        userInDb.Id.Should().NotBeEmpty(); 
    }
}
