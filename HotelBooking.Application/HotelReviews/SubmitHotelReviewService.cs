using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelReviews.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.HotelReviews;

public class SubmitHotelReviewService : ISubmitHotelReviewService
{
    private readonly IHotelReviewRepository _hotelReviewRepository;

    public SubmitHotelReviewService(IHotelReviewRepository hotelReviewRepository)
    {
        _hotelReviewRepository = hotelReviewRepository;
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
            throw new BadRequestException("You cannot review another customer's booking.");
        }

        if (booking.Room == null || booking.Room.HotelId != hotelId)
        {
            throw new BadRequestException("The booking does not belong to this hotel.");
        }

        if (booking.BookingStatus != BookingStatus.Completed)
        {
            throw new BadRequestException("Only completed bookings can be reviewed.");
        }

        if (booking.CheckOut > DateTime.UtcNow)
        {
            throw new BadRequestException("A review cannot be submitted before the stay is completed.");
        }

        if (booking.Review != null)
        {
            throw new BadRequestException("A review has already been submitted for this booking.");
        }

        var review = new Review(request.BookingId, request.Rating, request.Comment);
        await _hotelReviewRepository.AddReviewAsync(review);
        await _hotelReviewRepository.SaveChangesAsync();
    }
}