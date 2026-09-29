using HotelBooking.Application.Bookings.Expiration;
using HotelBooking.Application.Common.Settings;

namespace HotelBooking.API.BackgroundServices;

public sealed class ExpiredPendingBookingsWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BookingSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ExpiredPendingBookingsWorker> _logger;

    public ExpiredPendingBookingsWorker(
        IServiceScopeFactory scopeFactory,
        BookingSettings settings,
        TimeProvider timeProvider,
        ILogger<ExpiredPendingBookingsWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.EnableExpirationWorker)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.ExpirationCheckIntervalSeconds), _timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var totalExpired = await ProcessExpiredBookingsAsync();
                if (totalExpired > 0)
                {
                    _logger.LogInformation("Expired {ExpiredBookingCount} pending bookings.", totalExpired);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occurred while processing expired pending bookings.");
            }
        }
    }

    private async Task<int> ProcessExpiredBookingsAsync()
    {
        var totalExpired = 0;
        int expiredInBatch;

        do
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider
                .GetRequiredService<IExpirePendingBookingsService>();
            expiredInBatch = await service.ExpireAsync();
            totalExpired += expiredInBatch;
        } while (expiredInBatch == _settings.ExpirationBatchSize);

        return totalExpired;
    }
}
