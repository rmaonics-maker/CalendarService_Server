using AutoMapper;
using AutoMapper.QueryableExtensions;
using Calendar_Service.Data;
using Calendar_Service.Models;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Calendar_Service.Services;

public class PostgresAppointmentService(
    ApplicationDbContext context,
    IPublishEndpoint publishEndpoint,
    IMapper mapper,
    ILogger<PostgresAppointmentService> logger) : IAppointmentService
{
    public async Task<AppointmentDto> CreateAppointmentAsync(AppointmentDto evt, CancellationToken token = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);

        try
        {
            var appointment = mapper.Map<Appointment>(evt);
            appointment.CreatedAtUtc = DateTime.UtcNow;
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync(token);

            var userEmail = await context.Users
                .Where(u => u.Id == appointment.UserId)
                .Select(u => u.Email).FirstOrDefaultAsync(token);
            
            await publishEndpoint.Publish(new AppointmentCreatedEvent(
                AppointmentId: appointment.Id,
                Title: appointment.Title,
                UserId: appointment.UserId,
                UserEmail: userEmail,
                OccurredAt: appointment.CreatedAtUtc), token);
            
            await transaction.CommitAsync(token);
            logger.LogInformation("Создано событие: ID={AppointmentId}, Title='{Title}', UserId={UserId}", 
                appointment.Id, appointment.Title, appointment.UserId);
            return mapper.Map<AppointmentDto>(appointment);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(token);
            logger.LogError(e, "Ошибка при создании события {AppointmentId}.", evt.Id);
            throw;
        }
    }

    public async Task<AppointmentDto?> GetAppointmentAsync(Guid id, CancellationToken token = default)
    {
        var appointment = await context.Appointments.FirstOrDefaultAsync(a => a.Id == id, token);
        //logger.LogInformation("Прочитано событие: ID={{AppointmentId}}, Title='{Title}', UserId={UserId}", appointment.Id, appointment.Title, appointment.UserId);
        return appointment is null ? null : mapper.Map<AppointmentDto>(appointment);
    }

    public async Task<IEnumerable<AppointmentDto>> GetAppointmentsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken token = default)
    {
        var bookings = context.Appointments
            .AsNoTracking()
            .Where(b => b.Period.LowerBound == from.Date && b.Period.UpperBound == from.Date)
            .OrderBy(b => b.Period.LowerBound)
            .ProjectTo<AppointmentDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);
        
        return mapper.Map<IEnumerable<AppointmentDto>>(bookings);
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
                OccurredAt: appointment.Period.LowerBound), token);
                
            logger.LogInformation("Обновлено событие: ID={AppointmentId}, Title='{Title}', UserId={UserId}",
                appointment.Id, appointment.Title, appointment.UserId);
            
            return mapper.Map<AppointmentDto>(appointment);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(token);
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
            
            await publishEndpoint.Publish(new AppointmentDeletedEvent(
                AppointmentId: appointment.Id,
                UserId: appointment.UserId,
                OccurredAt: appointment.Period.LowerBound), token);

            await transaction.CommitAsync(token);
            
            logger.LogInformation("Удалено событие: ID={AppointmentId}", id);
            
            return true;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(token);
            logger.LogError(e, "Ошибка при удалении события {AppointmentId}", id);
            throw;
        }
    }
}