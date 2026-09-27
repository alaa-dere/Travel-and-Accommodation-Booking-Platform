using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Emails;
using HotelBooking.Application.Emails.Dtos;
using HotelBooking.Domain.Enums;

namespace HotelBooking.Application.Payments;

public sealed class PaymentWebhookService : IPaymentWebhookService
{
    private readonly IPaymentGateway _gateway;
    private readonly IPaymentRepository _payments;
    private readonly IBookingTransactionManager _transactionManager;
    private readonly ICartRepository _cartRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBookingConfirmationEmailService _emailService;

    public PaymentWebhookService(
        IPaymentGateway gateway,
        IPaymentRepository payments,
        IBookingTransactionManager transactionManager,
        ICartRepository cartRepository,
        IUserRepository userRepository,
        IBookingConfirmationEmailService emailService)
    {
        _gateway = gateway;
        _payments = payments;
        _transactionManager = transactionManager;
        _cartRepository = cartRepository;
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task HandleAsync(string payload, string signature)
    {
        var webhookEvent = _gateway.ParseWebhook(payload, signature);
        if (string.IsNullOrWhiteSpace(webhookEvent.ProviderPaymentId))
            return;

        BookingConfirmationEmailDto? confirmation = null;
        await _transactionManager.ExecuteSerializableAsync(async () =>
        {
            var payment = await _payments.GetByProviderPaymentIdAsync(webhookEvent.ProviderPaymentId);
            if (payment is null)
                throw new PaymentProviderUnavailableException(
                    "The payment is not available yet. Stripe should retry this webhook.");

            switch (webhookEvent.Status)
            {
                case PaymentGatewayStatus.Succeeded:
                    if (payment.Status == PaymentStatus.Paid)
                        return;

                    payment.MarkAsPaid();
                    confirmation = await CompletePaidCheckoutAsync(payment);
                    break;
                case PaymentGatewayStatus.Failed:
                    payment.MarkAsFailed(webhookEvent.FailureCode);
                    CancelBookings(payment);
                    break;
                case PaymentGatewayStatus.Cancelled:
                    payment.MarkAsCancelled();
                    CancelBookings(payment);
                    break;
                case PaymentGatewayStatus.RequiresAction:
                    payment.MarkAsRequiresAction();
                    break;
            }
        });

        if (confirmation is not null)
        {
            try
            {
                await _emailService.SendAsync(confirmation);
            }
            catch
            {
                // Payment is already committed; email delivery must not make Stripe retry.
            }
        }
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
            _cartRepository.DeleteRange(purchasedItems);

        var email = await _userRepository.GetEmailByIdAsync(invoice.UserId);
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return new BookingConfirmationEmailDto
        {
            CustomerEmail = email,
            InvoiceId = invoice.InvoiceId,
            HotelName = invoice.Hotel?.Name ?? "Hotel",
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

    private static void CancelBookings(Domain.Entities.Payment payment)
    {
        if (payment.Invoice is null)
            return;

        foreach (var booking in payment.Invoice.Bookings)
            booking.Cancel();
    }
}
