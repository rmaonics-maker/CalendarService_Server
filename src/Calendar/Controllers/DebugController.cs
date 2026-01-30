using Calendar_Service.Data;
using Calendar_Service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Calendar_Service.Controllers;

[ApiController]
[Route("debug")]
public class DebugController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;

    public DebugController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [HttpPost("concurrency-test")]
    public async Task<IActionResult> TestConcurrency()
    {
        var eventId = Guid.Empty;
        // Генерируем случайную комнату, чтобы избежать Exclusion Constraint (Овербукинга)
        // при повторном запуске теста.
        var randomRoomId = Random.Shared.Next(1, 1000000);

        // 1. ПОДГОТОВКА: Создаем событие
        using (var scope = _serviceProvider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await ctx.Users.FirstOrDefaultAsync();
            if (user == null) return BadRequest("Нужен хотя бы один юзер в базе. Создайте его через Postman или SQL.");

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                Title = "Original Title",
                RoomId = randomRoomId, // <--- ИЗМЕНЕНИЕ ЗДЕСЬ
                UserId = user.Id,
                Period = new NpgsqlTypes.NpgsqlRange<DateTime>(
                    DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                    DateTime.SpecifyKind(DateTime.UtcNow.AddHours(1), DateTimeKind.Unspecified)
                ),
                Version = 0
            };
            ctx.Bookings.Add(booking);
            await ctx.SaveChangesAsync();
            eventId = booking.Id;
        }

        // 2. ГОНКА: Эмулируем двух админов
        string resultLog = $"Created test event {eventId} in Room {randomRoomId}\n";

        using (var scopeA = _serviceProvider.CreateScope())
        using (var scopeB = _serviceProvider.CreateScope())
        {
            var contextA = scopeA.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var contextB = scopeB.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Оба загружают событие (Version = 1)
            var eventA = await contextA.Bookings.FindAsync(eventId);
            var eventB = await contextB.Bookings.FindAsync(eventId);

            resultLog += $"Admin A loaded version: {eventA!.Version}\n";
            resultLog += $"Admin B loaded version: {eventB!.Version}\n";

            eventA.Title = "Title by Admin A";
            eventB.Title = "Title by Admin B";

            // 3. Admin A сохраняет (Успех -> Version становится 2)
            await contextA.SaveChangesAsync();
            resultLog += "Admin A saved successfully.\n";

            // 4. Admin B пытается сохранить (У него в памяти Version = 1, а в базе уже 2)
            try
            {
                await contextB.SaveChangesAsync();
                resultLog += "Admin B saved... ОШИБКА! Должен был упасть Exception.\n";
            }
            catch (DbUpdateConcurrencyException ex)
            {
                resultLog += "SUCCESS: Admin B FAILED as expected! (DbUpdateConcurrencyException caught)\n";

                var entry = ex.Entries.Single();
                var databaseValues = await entry.GetDatabaseValuesAsync();
                var databaseTitle = (string)databaseValues!["Title"]!;

                resultLog += $"Conflict details: Database already has title '{databaseTitle}'";
            }
        }

        return Ok(resultLog);
    }
}