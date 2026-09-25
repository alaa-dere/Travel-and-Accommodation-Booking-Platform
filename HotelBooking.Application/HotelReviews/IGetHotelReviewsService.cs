using HotelBooking.Application.HotelReviews.Dtos;

namespace HotelBooking.Application.HotelReviews;

public interface IGetHotelReviewsService
{
    Task<HotelReviewsResponseDto> GetHotelReviewsAsync(int hotelId);
}