using HotelBooking.Application.Emails.Dtos;
using HotelBooking.Application.Messaging;
using MassTransit;

namespace HotelBooking.Infrastructure.Messaging;

public sealed class MassTransitBackgroundTaskPublisher : IBackgroundTaskPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitBackgroundTaskPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishRefundRetryAsync(int paymentId, string providerPaymentId) =>
        _publishEndpoint.Publish(new RetryPaymentRefund(paymentId, providerPaymentId));

    public Task PublishBookingConfirmationAsync(BookingConfirmationEmailDto confirmation) =>
        _publishEndpoint.Publish(new SendBookingConfirmation(confirmation));
}
