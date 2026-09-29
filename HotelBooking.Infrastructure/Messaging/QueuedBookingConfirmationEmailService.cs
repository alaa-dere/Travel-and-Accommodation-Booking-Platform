using HotelBooking.Application.Emails;
using HotelBooking.Application.Emails.Dtos;
using HotelBooking.Application.Messaging;

namespace HotelBooking.Infrastructure.Messaging;

public sealed class QueuedBookingConfirmationEmailService : IBookingConfirmationEmailService
{
    private readonly IBackgroundTaskPublisher _publisher;

    public QueuedBookingConfirmationEmailService(IBackgroundTaskPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task SendAsync(BookingConfirmationEmailDto confirmation) =>
        _publisher.PublishBookingConfirmationAsync(confirmation);
}
