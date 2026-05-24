using Calendar.Booking.API.Options;
using Microsoft.Extensions.Options;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace Calendar.Booking.API.Services;

public sealed class RedisDistributedLockService(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<DistributedLockOptions> options)
    : IDistributedLockService
{
    private readonly RedLockFactory _redLockFactory = RedLockFactory.Create(
        new List<RedLockMultiplexer> { new(connectionMultiplexer) });
    private readonly DistributedLockOptions _options = options.Value;

    public async Task<IDistributedLock?> AcquireAsync(string resource, CancellationToken token = default)
    {
        var redLock = await _redLockFactory.CreateLockAsync(
            resource,
            _options.LockExpiry,
            _options.LockWait,
            _options.LockRetry,
            token);
        
        return redLock.IsAcquired ? new RedisLock(redLock) : null;
    }

    private sealed class RedisLock(IRedLock redLock) : IDistributedLock
    {
        public bool IsAcquired => redLock.IsAcquired;
        public ValueTask DisposeAsync() => redLock.DisposeAsync();
    }
}