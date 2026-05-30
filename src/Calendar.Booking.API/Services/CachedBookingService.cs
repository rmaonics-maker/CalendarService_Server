using System.Text.Json;
using Calendar.Booking.API.Options;
using Calendar.Contracts.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Calendar.Booking.API.Services;

public sealed class CachedBookingService(
    IBookingService inner,
    IDistributedCache cache,
    IOptions<CacheOptions> options,
    ILogger<CachedBookingService> logger) : IBookingService
{
    private record BookingCacheEntry(Guid UserId, BookingDto Booking);
    
    public async Task<IEnumerable<BookingDto>> GetBookingsAsync(Guid userId, CancellationToken token = default)
    {
        var cacheKey = $"bookings:user:{userId}";
        return await GetOrCreateAsync(cacheKey, () => inner.GetBookingsAsync(userId, token), token);
    }

    public async Task<(Guid?, BookingDto?)> GetBookingAsync(Guid id, CancellationToken token = default)
    {
        var cacheKey = $"booking:{id}";
        var cached = await cache.GetStringAsync(cacheKey, token);
        if (!string.IsNullOrEmpty(cached))
        {
            var entry = JsonSerializer.Deserialize<BookingCacheEntry>(cached);
            logger.LogDebug("Cache HIT: {Key}", cacheKey);
            return (entry?.UserId, entry?.Booking);
        }

        var (userId, booking) = await inner.GetBookingAsync(id, token);

        if (userId.HasValue && booking is not null)
        {
            var entryToCache = new BookingCacheEntry(userId.Value, booking);
            var serialized = JsonSerializer.Serialize(entryToCache);
            await SetWithJitterAsync(cacheKey, serialized, token);
            logger.LogDebug("Cache MISS: {Key}", cacheKey);
        }
        
        return (userId, booking);
    }
    
    public async Task<BookingDto> CreateBookingAsync(Guid userId, BookingDto bookingDto, CancellationToken token = default)
    {
        var result = await inner.CreateBookingAsync(userId, bookingDto, token);
        await cache.RemoveAsync($"bookings:user:{userId}", token);
        return result;
    }

    public async Task<bool> DeleteBookingAsync(Guid id, CancellationToken token = default)
    {
        var(_, booking) = await inner.GetBookingAsync(id, token);
        var result = await inner.DeleteBookingAsync(id, token);
        if (result && booking is not null)
        {
            await cache.RemoveAsync($"booking:{id}", token);
            //await cache.RemoveAsync($"booking:user:{booking.UserId}", token);
        }
        return result;
    }

    public async Task<BookingDto?> UpdateBookingAsync(Guid id, BookingDto bookingDto, CancellationToken token = default)
    {
        var result = await inner.UpdateBookingAsync(id, bookingDto, token);
        if (result is not null)
        {
            await cache.RemoveAsync($"booking:{id}", token);
            //await cache.RemoveAsync($"booking:user:{booking.UserId}", token);
        }
        return result;
    }

    private async Task<IEnumerable<BookingDto>> GetOrCreateAsync(string key,
        Func<Task<IEnumerable<BookingDto>>> factory, CancellationToken token)
    {
        var cached = await cache.GetStringAsync(key, token);
        if (cached is not null)
        {
            logger.LogDebug("Cache HIT: {Key}", key);
            return JsonSerializer.Deserialize<List<BookingDto>>(cached) ?? [];
        }
        logger.LogDebug("Cache MISS: {Key}", key);
        var result = (await factory()).ToArray();
        await SetWithJitterAsync(key, JsonSerializer.Serialize(result), token);
        return result;
    }
    
    private async Task SetWithJitterAsync(string key, string value, CancellationToken token)
    {
        var jitter = Random.Shared.Next(0, options.Value.JitterMinutes * 60) / 60.0;
        var ttl = TimeSpan.FromMinutes(options.Value.BaseTltMinutes + jitter);
        
        var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
        await cache.SetStringAsync(key, value, cacheOptions, token);
    }
}