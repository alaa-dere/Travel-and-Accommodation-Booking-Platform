using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Payments;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;

namespace HotelBooking.Application.Bookings.Cancel;

public class CancelBookingService : ICancelBookingService
{
    private readonly IBookingRepository _bookings;
    private readonly IPaymentGateway _paymentGateway;
    private readonly TimeProvider _timeProvider;

    public CancelBookingService(IBookingRepository bookings, IPaymentGateway paymentGateway, TimeProvider timeProvider)
    {
        _bookings = bookings;
        _paymentGateway = paymentGateway;
        _timeProvider = timeProvider;
    }

    public async Task CancelAsync(int bookingId, int userId)
    {
        if (bookingId <= 0 || userId <= 0)
        {
            throw new BadRequestException("Invalid booking or user ID.");
        }

        var booking = await _bookings.GetByIdForUserAsync(bookingId, userId);
        if (booking == null)
        {
            throw new NotFoundException("Booking not found.");
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        EnsureCanBeCancelled(booking, utcNow);

        var payment = booking.Invoice?.Payment;
        if (payment == null)
        {
            booking.CancelByCustomer(utcNow);
            await _bookings.SaveChangesAsync();
            return;
        }

        if (payment.Status != PaymentStatus.Paid || string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
        {
            throw new ConflictException("A booking cannot be cancelled while its payment is still being processed.");
        }

        var refund = await _paymentGateway.RefundAsync(
            payment.ProviderPaymentId,
            $"booking-cancellation:{booking.BookingId}",
            booking.TotalPrice);

        if (refund.Status is RefundGatewayStatus.Failed or RefundGatewayStatus.Cancelled)
        {
            throw new PaymentProviderUnavailableException("Stripe did not accept the booking refund.");
        }

        booking.RecordCustomerRefund(booking.TotalPrice, refund.ProviderRefundId, MapRefundStatus(refund.Status), refund.FailureCode, utcNow);
        booking.CancelByCustomer(utcNow);

        if (booking.Invoice!.Bookings.All(item => item.BookingId == booking.BookingId || item.BookingStatus == BookingStatus.Cancelled))
        {
            payment.RecordRefund(
                refund.ProviderRefundId,
                MapRefundStatus(refund.Status),
                refund.FailureCode,
                utcNow);
        }

        await _bookings.SaveChangesAsync();
    }

    private static void EnsureCanBeCancelled(Booking booking, DateTime utcNow)
    {
        if (booking.BookingStatus == BookingStatus.Cancelled)
        {
            throw new ConflictException("Booking is already cancelled.");
        }
        if (utcNow >= booking.CheckIn)
        {
            throw new ConflictException("Booking cannot be cancelled after the stay has started.");
        }
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
