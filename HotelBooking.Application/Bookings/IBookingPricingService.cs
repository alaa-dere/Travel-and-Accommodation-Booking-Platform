using HotelBooking.Application.Bookings.Dtos;

namespace HotelBooking.Application.Bookings;

public interface IBookingPricingService
{
    Task<BookingPriceResultDto> CalculatePriceAsync(int roomId, DateTime checkIn, DateTime checkOut, DateTime bookingCreationTime);
}