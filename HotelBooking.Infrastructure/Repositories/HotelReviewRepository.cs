using HotelBooking.Application.HotelReviews.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;
 
public class HotelReviewRepository : IHotelReviewRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public HotelReviewRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<ReviewResponseDto>> GetReviewsByHotelIdAsync(int hotelId)

    {
        return _dbContext.Reviews.AsNoTracking()
            .Where(review => review.Booking != null && review.Booking.Room != null && review.Booking.Room.HotelId == hotelId)
            .OrderByDescending(review => review.CreatedAt)
            .Select(review => new ReviewResponseDto
            {
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            })
           .ToListAsync();
    }
}