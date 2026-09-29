using HotelBooking.Application.Emails.Dtos;

namespace HotelBooking.Application.Messaging;

public interface IBackgroundTaskPublisher
{
    Task PublishRefundRetryAsync(int paymentId, string providerPaymentId);
    Task PublishBookingConfirmationAsync(BookingConfirmationEmailDto confirmation);
}
