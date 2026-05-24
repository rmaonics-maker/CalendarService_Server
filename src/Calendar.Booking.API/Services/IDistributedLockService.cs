namespace Calendar.Booking.API.Services;

public interface IDistributedLockService
{
    Task<IDistributedLock?> AcquireAsync(string resource, CancellationToken token = default);
}

public interface IDistributedLock : IAsyncDisposable
{
    bool IsAcquired { get; }
}