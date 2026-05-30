using System.Diagnostics;
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
        using var activity = Telemetry.ActivitySource.StartActivity("distributed_lock.acquire");
        activity?.SetTag("lock.resource", resource);

        var sw = Stopwatch.StartNew();
        var redLock = await _redLockFactory.CreateLockAsync(
            resource,
            _options.LockExpiry,
            _options.LockWait,
            _options.LockRetry,
            token);
        sw.Stop();
        
        Telemetry.LockWaitDuration.Record(
            sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("resource", resource));

        if (redLock.IsAcquired)
        {
            Telemetry.LockAcquiredCount.Add(1);
            activity?.SetTag("lock.acquired", true);
            activity?.SetTag("lock.wait_ms", sw.Elapsed.TotalMilliseconds);
            return new RedisLock(redLock);
        }
        
        Telemetry.LockFailedCount.Add(1);
        activity?.SetTag("lock.acquired", false);
        activity?.SetStatus(ActivityStatusCode.Error, "Failed to acquire lock");
        return null;
    }

    private sealed class RedisLock(IRedLock redLock) : IDistributedLock
    {
        public bool IsAcquired => redLock.IsAcquired;
        public ValueTask DisposeAsync() => redLock.DisposeAsync();
    }
}