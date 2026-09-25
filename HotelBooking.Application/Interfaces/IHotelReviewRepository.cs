using HotelBooking.Application.HotelReviews.Dtos;

namespace HotelBooking.Application.Interfaces;

public interface IHotelReviewRepository
{
    Task<List<ReviewResponseDto>> GetReviewsByHotelIdAsync(int hotelId);
}