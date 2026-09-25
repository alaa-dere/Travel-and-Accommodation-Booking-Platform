using HotelBooking.Application.Emails.Dtos;

namespace HotelBooking.Application.Emails;

public interface IBookingConfirmationEmailService
{
    Task SendAsync(BookingConfirmationEmailDto confirmation);
}