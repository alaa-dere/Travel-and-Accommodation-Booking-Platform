using HotelBooking.Application.HotelReviews.Dtos;

namespace HotelBooking.Application.HotelReviews;

public interface ISubmitHotelReviewService
{
    Task SubmitReviewAsync(int hotelId, int userId, SubmitReviewRequestDto request);
}