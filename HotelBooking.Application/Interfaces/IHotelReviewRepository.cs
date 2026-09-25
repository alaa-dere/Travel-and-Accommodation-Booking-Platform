using HotelBooking.Application.HotelReviews.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IHotelReviewRepository
{
    Task<List<ReviewResponseDto>> GetReviewsByHotelIdAsync(int hotelId);
    Task<Booking?> GetBookingForReviewAsync(int bookingId);
    Task AddReviewAsync(Review review);
    Task SaveChangesAsync();
}