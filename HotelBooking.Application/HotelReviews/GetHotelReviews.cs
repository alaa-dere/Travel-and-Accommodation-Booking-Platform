using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelReviews.Dtos;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.HotelReviews;

public class GetHotelReviews : IGetHotelReviewsService
{
    private readonly IHotelReviewRepository _reviewRepository;
    private readonly IHotelRepository _hotelRepository;

    public GetHotelReviews(IHotelReviewRepository reviewRepository, IHotelRepository hotelRepository)
    {
        _reviewRepository = reviewRepository;
        _hotelRepository = hotelRepository;
    }

    public async Task<HotelReviewsResponseDto> GetHotelReviewsAsync(int hotelId)
    {
        var isActive = await _hotelRepository.IsActiveHotelAsync(hotelId);
        if (!isActive)
        {
            throw new NotFoundException("Hotel not found");
        }
        
        var reviews = await _reviewRepository.GetReviewsByHotelIdAsync(hotelId);
        double? rating = reviews.Count == 0 ? null : reviews.Average(review => review.Rating);
        return new HotelReviewsResponseDto
        {
            Rating = rating,
            Reviews = reviews
        };
    }
}