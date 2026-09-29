using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelReviews.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.HotelReviews;

public class SubmitHotelReviewService : ISubmitHotelReviewService
{
    private readonly IHotelReviewRepository _hotelReviewRepository;
    private readonly TimeProvider _timeProvider;

    public SubmitHotelReviewService(IHotelReviewRepository hotelReviewRepository, TimeProvider timeProvider)
    {
        _hotelReviewRepository = hotelReviewRepository;
        _timeProvider = timeProvider;
    }

    public SubmitHotelReviewService(IHotelReviewRepository hotelReviewRepository) : this(hotelReviewRepository, TimeProvider.System)
    {
    }

    public async Task SubmitReviewAsync(int hotelId, int userId, SubmitReviewRequestDto request)
    {
        if (request.Rating < 1 || request.Rating > 5)
        {
            throw new BadRequestException("Rating must be between 1 and 5.");
        }

        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new BadRequestException("Comment must not be empty.");
        }

        var booking = await _hotelReviewRepository.GetBookingForReviewAsync(request.BookingId);
        if (booking == null)
        {
            throw new NotFoundException("Booking not found.");
        }

        if (booking.UserId != userId)
        {
            throw new NotFoundException("Booking not found.");
        }

        if (booking.Room == null || booking.Room.HotelId != hotelId)
        {
            throw new BadRequestException("The booking does not belong to this hotel.");
        }

        if (booking.BookingStatus != BookingStatus.Completed)
        {
            throw new BadRequestException("Only completed bookings can be reviewed.");
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        if (booking.CheckOut > utcNow)
        {
            throw new BadRequestException("A review cannot be submitted before the stay is completed.");
        }

        if (booking.Review != null)
        {
            throw new BadRequestException("A review has already been submitted for this booking.");
        }

        var review = new Review(request.BookingId, request.Rating, request.Comment, utcNow);
        await _hotelReviewRepository.AddReviewAsync(review);
        await _hotelReviewRepository.SaveChangesAsync();
    }
}
