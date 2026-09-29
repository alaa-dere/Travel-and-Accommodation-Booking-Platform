using HotelBooking.Application.Common.Settings;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Payments;
using HotelBooking.Application.Messaging;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;

namespace HotelBooking.Application.Bookings.Expiration;

public sealed class ExpirePendingBookingsService : IExpirePendingBookingsService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly TimeProvider _timeProvider;
    private readonly BookingSettings _settings;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IBackgroundTaskPublisher _backgroundTasks;

    public ExpirePendingBookingsService(
        IBookingRepository bookingRepository,
        TimeProvider timeProvider,
        BookingSettings settings,
        IPaymentGateway paymentGateway,
        IBackgroundTaskPublisher backgroundTasks)
    {
        _bookingRepository = bookingRepository;
        _timeProvider = timeProvider;
        _settings = settings;
        _paymentGateway = paymentGateway;
        _backgroundTasks = backgroundTasks;
    }

    public async Task<int> ExpireAsync()
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var bookings = await _bookingRepository.GetExpiredPendingBookingsAsync(utcNow, _settings.ExpirationBatchSize);

        var expiredCount = 0;
        var hasChanges = false;

        foreach (var group in bookings.GroupBy(booking => booking.Invoice?.Payment))
        {
            var payment = group.Key;
            if (payment is null)
            {
                expiredCount += ExpireBookings(group, utcNow);
                hasChanges = true;
                continue;
            }

            if (payment.Status is PaymentStatus.Cancelled or PaymentStatus.Failed or PaymentStatus.Refunded)
            {
                expiredCount += ExpireBookings(group, utcNow);
                hasChanges = true;
                continue;
            }

            if (payment.Status == PaymentStatus.Paid)
            {
                await RefundAndExpireAsync(payment, group, utcNow);
                expiredCount += group.Count();
                hasChanges = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
            {
                payment.MarkAsCancelled(utcNow);
                expiredCount += ExpireBookings(group, utcNow);
                hasChanges = true;
                continue;
            }

            var providerStatus = await _paymentGateway.CancelAsync(payment.ProviderPaymentId);
            if (providerStatus == PaymentGatewayStatus.Cancelled)
            {
                payment.MarkAsCancelled(utcNow);
                expiredCount += ExpireBookings(group, utcNow);
                hasChanges = true;
            }
            else if (providerStatus == PaymentGatewayStatus.Succeeded)
            {
                payment.MarkAsPaid(utcNow);
                await RefundAndExpireAsync(payment, group, utcNow);
                expiredCount += group.Count();
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _bookingRepository.SaveChangesAsync();
        }

        return expiredCount;
    }

    private static int ExpireBookings(IEnumerable<Booking> bookings, DateTime utcNow)
    {
        var count = 0;
        foreach (var booking in bookings)
        {
            booking.Expire(utcNow);
            count++;
        }

        return count;
    }

    private async Task RefundAndExpireAsync(Payment payment, IEnumerable<Booking> bookings, DateTime utcNow)
    {
        var providerPaymentId = payment.ProviderPaymentId
            ?? throw new InvalidOperationException("A paid payment must have a provider payment ID.");
        var result = await _paymentGateway.RefundAsync(providerPaymentId, $"expired-booking-refund:{providerPaymentId}");

        if (result.Status is RefundGatewayStatus.Failed or RefundGatewayStatus.Cancelled)
        {
            await _backgroundTasks.PublishRefundRetryAsync(payment.PaymentId, providerPaymentId);
        }

        payment.RecordRefund(
            result.ProviderRefundId,
            MapRefundStatus(result.Status),
            result.FailureCode,
            utcNow);
        ExpireBookings(bookings, utcNow);
    }

    private static RefundStatus MapRefundStatus(RefundGatewayStatus status) => status switch
    {
        RefundGatewayStatus.RequiresAction => RefundStatus.RequiresAction,
        RefundGatewayStatus.Succeeded => RefundStatus.Succeeded,
        RefundGatewayStatus.Failed => RefundStatus.Failed,
        RefundGatewayStatus.Cancelled => RefundStatus.Cancelled,
        _ => RefundStatus.Pending
    };
}
