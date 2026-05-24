using AutoMapper;
using Calendar_Service.Data;
using Calendar_Service.Models;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Events;
using Calendar.Contracts.Types;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Calendar_Service.Services;

public class PostgresAppointmentService(
    ApplicationDbContext context,
    IPublishEndpoint publishEndpoint,
    IMapper mapper,
    ILogger<PostgresAppointmentService> logger,
    TimeProvider timeProvider,
    IRecurrenceEvaluator recurrenceEvaluator) : IAppointmentService
{
    public async Task<AppointmentDto> CreateAppointmentAsync(AppointmentDto evt, CancellationToken token = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);

        try
        {
            var appointment = mapper.Map<Appointment>(evt);
            appointment.CreatedAt = timeProvider.GetUtcNow();
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync(token);

            var userEmail = await context.Users
                .Where(u => u.Id == appointment.UserId)
                .Select(u => u.Email).FirstOrDefaultAsync(token);

            await transaction.CommitAsync(token);

            await publishEndpoint.Publish(new AppointmentCreatedEvent(
                AppointmentId: appointment.Id,
                Title: appointment.Title,
                UserId: appointment.UserId,
                UserEmail: userEmail,
                OccurredAt: appointment.CreatedAt), token);

            logger.LogInformation("Создано событие: ID={AppointmentId}, Title='{Title}', UserId={UserId}",
                appointment.Id, appointment.Title, appointment.UserId);
            return mapper.Map<AppointmentDto>(appointment);
        }
        catch (Exception e)
        {
            try { await transaction.RollbackAsync(token); } catch { }
            logger.LogError(e, "Ошибка при создании события {AppointmentId}.", evt.Id);
            throw;
        }
    }

    public async Task<AppointmentDto?> GetAppointmentAsync(Guid id, CancellationToken token = default)
    {
        var appointment = await context.Appointments.FindAsync([id], token);
        return appointment is null ? null : mapper.Map<AppointmentDto>(appointment);
    }

    public async Task<IEnumerable<AppointmentDto>> GetAppointmentsAsync(
        DateTimeOffset from, 
        DateTimeOffset to, 
        CancellationToken token = default)
    {
        var fromInstant = Instant.FromDateTimeOffset(from);
        var toInstant = Instant.FromDateTimeOffset(to);

        // 1. Загружаем всех "мастеров", которые могут иметь вхождения в этом диапазоне
        var masters = await context.Appointments
            .AsNoTracking()
            .Where(a => a.Slot.Start <= toInstant && 
                        (a.Recurrence == null || a.Recurrence.Until == null || a.Recurrence.Until >= fromInstant))
            .ToListAsync(token);

        var masterIds = masters.Select(m => m.Id).ToList();

        // 2. Загружаем все исключения для этих мастеров
        var exceptions = await context.Exceptions
            .AsNoTracking()
            .Where(e => masterIds.Contains(e.MasterAppointmentId))
            .ToListAsync(token);

        var exceptionMap = exceptions.GroupBy(e => e.MasterAppointmentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var range = new TimeSlot(fromInstant, toInstant);
        var allOccurrences = new List<AppointmentDto>();

        // 3. Разворачиваем каждого мастера
        foreach (var master in masters)
        {
            var masterExceptions = exceptionMap.TryGetValue(master.Id, out var ex) ? ex : new List<AppointmentException>();
            var expanded = recurrenceEvaluator.GetOccurrences(master, masterExceptions, range);
            allOccurrences.AddRange(expanded.Events);
        }

        return allOccurrences.OrderBy(o => o.StartTime);
    }
    

    public async Task<AppointmentDto?> UpdateAppointmentAsync(Guid id, AppointmentDto request, CancellationToken token = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);

        try
        {
            var appointment = await context.Appointments.FirstOrDefaultAsync(a => a.Id == id, token);
            if (appointment is null) return null;
            mapper.Map(request, appointment);
            context.Appointments.Update(appointment);
            await context.SaveChangesAsync(token);
            await transaction.CommitAsync(token);

            await publishEndpoint.Publish(new AppointmentUpdatedEvent(
                AppointmentId: appointment.Id,
                NewTitle: appointment.Title,
                OccurredAt: appointment.Slot.Start.ToDateTimeUtc()), token);
                
            logger.LogInformation("Обновлено событие: ID={AppointmentId}, Title='{Title}', UserId={UserId}",
                appointment.Id, appointment.Title, appointment.UserId);
            
            return mapper.Map<AppointmentDto>(appointment);
        }
        catch (Exception e)
        {
            try { await transaction.RollbackAsync(token); } catch { }
            logger.LogError(e, "Ошибка при обновлении события {AppointmentId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAppointmentAsync(Guid id, CancellationToken token = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);

        try
        {
            var appointment = await context.Appointments.FirstOrDefaultAsync(a => a.Id == id, token);

            if(appointment is null) return false;

            context.Appointments.Remove(appointment);
            await context.SaveChangesAsync(token);

            await transaction.CommitAsync(token);

            await publishEndpoint.Publish(new AppointmentDeletedEvent(
                AppointmentId: appointment.Id,
                UserId: appointment.UserId,
                OccurredAt: appointment.Slot.Start.ToDateTimeUtc()), token);

            logger.LogInformation("Удалено событие: ID={AppointmentId}", id);

            return true;
        }
        catch (Exception e)
        {
            try { await transaction.RollbackAsync(token); } catch { }
            logger.LogError(e, "Ошибка при удалении события {AppointmentId}", id);
            throw;
        }
    }

    public async Task<List<AppointmentException>> GetExceptionsAsync(Guid masterId, CancellationToken token = default)
    {
        return await context.Exceptions
            .AsNoTracking()
            .Where(e => e.MasterAppointmentId == masterId)
            .ToListAsync(token);
    }
}