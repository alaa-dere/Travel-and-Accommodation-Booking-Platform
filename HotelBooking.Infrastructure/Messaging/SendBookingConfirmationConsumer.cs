using HotelBooking.Application.Emails;
using HotelBooking.Application.Messaging;
using MassTransit;

namespace HotelBooking.Infrastructure.Messaging;

public sealed class SendBookingConfirmationConsumer : IConsumer<SendBookingConfirmation>
{
    private readonly BookingConfirmationEmailService _emailService;

    public SendBookingConfirmationConsumer(BookingConfirmationEmailService emailService)
    {
        _emailService = emailService;
    }

    public Task Consume(ConsumeContext<SendBookingConfirmation> context) =>
        _emailService.SendAsync(context.Message.Confirmation);
}
