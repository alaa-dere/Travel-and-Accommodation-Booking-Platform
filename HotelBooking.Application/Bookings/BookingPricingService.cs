using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Bookings;

public class BookingPricingService : IBookingPricingService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IPromotionRepository _promotionRepository;

    public BookingPricingService(IRoomRepository roomRepository, IPromotionRepository promotionRepository)
    {
        _roomRepository = roomRepository;
        _promotionRepository = promotionRepository;
    }

    public async Task<BookingPriceResultDto> CalculatePriceAsync(int roomId, DateTime checkIn, DateTime checkOut, DateTime bookingCreationTime)
    {
        if (checkOut.Date <= checkIn.Date)
        {
            throw new BadRequestException("Check-out date must be after check-in date.");
        }
        var room = await _roomRepository.GetRoomByIdAsync(roomId);

        if (room == null)
        {
            throw new NotFoundException("Room not found.");
        }
        var numberOfNights = (checkOut.Date - checkIn.Date).Days;
        var pricePerNight = room.PricePerNight;
        var originalTotalPrice = pricePerNight * numberOfNights;
        var promotion = await _promotionRepository.GetActivePromotionForHotelAsync(room.HotelId, bookingCreationTime);
        var discountPercentage = promotion?.DiscountPercentage ?? 0;
        var discountAmount = originalTotalPrice * discountPercentage / 100m;
        var totalPrice = originalTotalPrice - discountAmount;

        return new BookingPriceResultDto
        {
            PricePerNight = pricePerNight,
            NumberOfNights = numberOfNights,
            OriginalTotalPrice = originalTotalPrice,
            DiscountPercentage = discountPercentage,
            DiscountAmount = discountAmount,
            TotalPrice = totalPrice
        };
    }
}