using HotelBooking.Application.Emails.Dtos;

namespace HotelBooking.Application.Messaging;

public sealed record SendBookingConfirmation(BookingConfirmationEmailDto Confirmation);
