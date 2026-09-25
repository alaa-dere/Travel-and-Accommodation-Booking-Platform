using HotelBooking.Application.Bookings.Dtos;

namespace HotelBooking.Application.Bookings;

public interface ICreateBookingsService
{
    Task<BookingCreationResultDto> CreateBookingsAsync(int userId);
}