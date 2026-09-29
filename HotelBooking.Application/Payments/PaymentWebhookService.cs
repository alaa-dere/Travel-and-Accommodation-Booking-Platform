using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Emails;
using HotelBooking.Application.Emails.Dtos;
using HotelBooking.Domain.Enums;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Payments;

public sealed class PaymentWebhookService : IPaymentWebhookService
{
    private readonly IPaymentGateway _gateway;
    private readonly IPaymentRepository _payments;
    private readonly IBookingRepository _bookings;
    private readonly IBookingTransactionManager _transactionManager;
    private readonly ICartRepository _cartRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBookingConfirmationEmailService _emailService;
    private readonly TimeProvider _timeProvider;

    public PaymentWebhookService(
        IPaymentGateway gateway,
        IPaymentRepository payments,
        IBookingRepository bookings,
        IBookingTransactionManager transactionManager,
        ICartRepository cartRepository,
        IUserRepository userRepository,
        IBookingConfirmationEmailService emailService,
        TimeProvider timeProvider)
    {
        _gateway = gateway;
        _payments = payments;
        _bookings = bookings;
        _transactionManager = transactionManager;
        _cartRepository = cartRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _timeProvider = timeProvider;
    }

    public async Task HandleAsync(string payload, string signature)
    {
        var webhookEvent = _gateway.ParseWebhook(payload, signature);
        if (webhookEvent.Type == PaymentProviderWebhookEventType.Ignored ||
            string.IsNullOrWhiteSpace(webhookEvent.ProviderObjectId))
            return;

        if (webhookEvent.Type == PaymentProviderWebhookEventType.Refund)
        {
            await HandleRefundAsync(webhookEvent);
            return;
        }

        BookingConfirmationEmailDto? confirmation = null;
        LatePaymentRefund? latePaymentRefund = null;
        await _transactionManager.ExecuteSerializableAsync(async () =>
        {
            var payment = await _payments.GetByProviderPaymentIdAsync(webhookEvent.ProviderObjectId);
            if (payment is null && webhookEvent.InternalPaymentId is int paymentId)
                payment = await _payments.GetByIdAsync(paymentId);

            if (payment is null)
                throw new PaymentProviderUnavailableException(
                    "The payment is not available yet. Stripe should retry this webhook.");

            if (string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
            {
                payment.AttachProviderPayment(
                    webhookEvent.ProviderObjectId,
                    _gateway.Currency,
                    clientSecret: null);
            }

            switch (webhookEvent.PaymentStatus)
            {
                case PaymentGatewayStatus.Succeeded:
                    if (payment.Status == PaymentStatus.Refunded || payment.RefundStatus is not null)
                        return;

                    var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
                    if (RequiresRefund(payment, utcNow))
                    {
                        payment.MarkAsPaid(utcNow);
                        latePaymentRefund = new LatePaymentRefund(payment, utcNow);
                        break;
                    }

                    if (payment.Status == PaymentStatus.Paid)
                        return;

                    payment.MarkAsPaid(utcNow);
                    ConfirmBookings(payment, utcNow);
                    confirmation = await CompletePaidCheckoutAsync(payment);
                    break;
                case PaymentGatewayStatus.Failed:
                    if (payment.Status is PaymentStatus.Paid
                        or PaymentStatus.Failed
                        or PaymentStatus.Cancelled
                        or PaymentStatus.Refunded)
                        return;

                    var failedAt = _timeProvider.GetUtcNow().UtcDateTime;
                    payment.MarkAsFailed(webhookEvent.FailureCode, failedAt);
                    CancelBookings(payment, failedAt);
                    break;
                case PaymentGatewayStatus.Cancelled:
                    if (payment.Status is PaymentStatus.Paid
                        or PaymentStatus.Failed
                        or PaymentStatus.Cancelled
                        or PaymentStatus.Refunded)
                        return;

                    var cancelledAt = _timeProvider.GetUtcNow().UtcDateTime;
                    payment.MarkAsCancelled(cancelledAt);
                    CancelBookings(payment, cancelledAt);
                    break;
                case PaymentGatewayStatus.RequiresAction:
                    payment.MarkAsRequiresAction();
                    break;
            }
        });

        if (latePaymentRefund is not null)
        {
            await RefundLatePaymentAsync(latePaymentRefund);
        }
        if (confirmation is not null)
        {
            try
            {
                await _emailService.SendAsync(confirmation);
            }
            catch
            {
            }
        }
    }

    private Task HandleRefundAsync(PaymentProviderWebhookEvent webhookEvent)
    {
        return _transactionManager.ExecuteSerializableAsync(async () =>
        {
            var payment = await _payments.GetByProviderRefundIdAsync(webhookEvent.ProviderObjectId);
            var booking = await _bookings.GetByProviderRefundIdAsync(webhookEvent.ProviderObjectId);
            if (payment is null && booking is null)
            {
                throw new PaymentProviderUnavailableException("The refund is not available yet. Stripe should retry this webhook.");
            }

            var refundStatus = MapRefundStatus(webhookEvent.RefundStatus);
            var processedAt = _timeProvider.GetUtcNow().UtcDateTime;
            payment?.RecordRefund(webhookEvent.ProviderObjectId, refundStatus, webhookEvent.FailureCode, processedAt);
            if (booking?.RefundedAmount is decimal refundedAmount)
            {
                booking.RecordCustomerRefund(refundedAmount, webhookEvent.ProviderObjectId, refundStatus, webhookEvent.FailureCode, processedAt);
            }
        });
    }

    private static RefundStatus MapRefundStatus(RefundGatewayStatus? status) => status switch
    {
        RefundGatewayStatus.RequiresAction => RefundStatus.RequiresAction,
        RefundGatewayStatus.Succeeded => RefundStatus.Succeeded,
        RefundGatewayStatus.Failed => RefundStatus.Failed,
        RefundGatewayStatus.Cancelled => RefundStatus.Cancelled,
        _ => RefundStatus.Pending
    };

    private static bool RequiresRefund(Domain.Entities.Payment payment, DateTime utcNow)
    {
        return payment.Invoice?.Bookings.Any(booking =>
            booking.BookingStatus == BookingStatus.Cancelled ||
            booking.BookingStatus == BookingStatus.Pending && booking.PendingExpiresAt <= utcNow) == true;
    }

    private async Task RefundLatePaymentAsync(LatePaymentRefund pendingRefund)
    {
        var payment = pendingRefund.Payment;
        var providerPaymentId = payment.ProviderPaymentId
            ?? throw new InvalidOperationException("A paid payment must have a provider payment ID.");

        var refund = await _gateway.RefundAsync(providerPaymentId, $"expired-booking-refund:{providerPaymentId}");

        await _transactionManager.ExecuteSerializableAsync(() =>
        {
            payment.RecordRefund(refund.ProviderRefundId, MapRefundStatus(refund.Status), refund.FailureCode, pendingRefund.ProcessedAt);

            if (payment.Invoice is not null)
            {
                foreach (var booking in payment.Invoice.Bookings.Where(booking =>
                             booking.BookingStatus == BookingStatus.Pending &&
                             booking.PendingExpiresAt <= pendingRefund.ProcessedAt))
                {
                    booking.Expire(pendingRefund.ProcessedAt);
                }
            }

            return Task.CompletedTask;
        });
    }

    private async Task<BookingConfirmationEmailDto?> CompletePaidCheckoutAsync(Domain.Entities.Payment payment)
    {
        var invoice = payment.Invoice;
        if (invoice is null)
            return null;

        var cartItems = await _cartRepository.GetByUserIdAsync(invoice.UserId);
        var bookedKeys = invoice.Bookings
            .Select(booking => (booking.RoomId, booking.CheckIn, booking.CheckOut))
            .ToHashSet();
        var purchasedItems = cartItems
            .Where(item => bookedKeys.Contains((item.RoomId, item.CheckIn, item.CheckOut)))
            .ToList();
        if (purchasedItems.Count > 0)
        {
            _cartRepository.DeleteRange(purchasedItems);
        }
        var email = await _userRepository.GetEmailByIdAsync(invoice.UserId);
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return new BookingConfirmationEmailDto
        {
            CustomerEmail = email,
            InvoiceId = invoice.InvoiceId,
            HotelName = invoice.Hotel?.Name ?? "Hotel",
            HotelAddress = invoice.Hotel?.Address ?? string.Empty,
            InvoiceTotal = invoice.TotalAmount,
            PaymentStatus = payment.Status,
            Rooms = invoice.Bookings.Select(booking => new BookingConfirmationEmailRoomDto
            {
                BookingId = booking.BookingId,
                RoomNumber = booking.Room?.RoomNumber ?? booking.RoomId.ToString(),
                CheckIn = booking.CheckIn,
                CheckOut = booking.CheckOut,
                TotalPrice = booking.TotalPrice
            }).ToList()
        };
    }

    private static void CancelBookings(Domain.Entities.Payment payment, DateTime utcNow)
    {
        if (payment.Invoice is null)
            return;

        foreach (var booking in payment.Invoice.Bookings.Where(booking =>
                     booking.BookingStatus is BookingStatus.Pending or BookingStatus.Confirmed))
        {
            booking.Cancel(utcNow);
        }
    }

    private static void ConfirmBookings(Domain.Entities.Payment payment, DateTime utcNow)
    {
        if (payment.Invoice is null)
            return;

        foreach (var booking in payment.Invoice.Bookings)
        {
            if (booking.BookingStatus == BookingStatus.Pending)
            {
                booking.Confirm(utcNow);
            }
            
        }
    }

    private sealed record LatePaymentRefund(Domain.Entities.Payment Payment, DateTime ProcessedAt);
}
