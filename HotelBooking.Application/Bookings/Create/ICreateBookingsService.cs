using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Payments.Dtos;

namespace HotelBooking.Application.Bookings;

public interface ICreateBookingsService
{
    Task<BookingCreationResultDto> CreateBookingsAsync(int userId, string? specialRequests, PaymentInformationDto paymentInformation);
}