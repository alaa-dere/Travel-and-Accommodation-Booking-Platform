using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Bookings.Modify;

public class ModifyBookingService : IModifyBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingAvailabilityService _availabilityService;
    private readonly IBookingPricingService _pricingService;
    private readonly IBookingTransactionManager _transactionManager;
    private readonly IRoomRepository _roomRepository;

    public ModifyBookingService(
        IBookingRepository bookingRepository,
        IBookingAvailabilityService availabilityService,
        IBookingPricingService pricingService,
        IBookingTransactionManager transactionManager,
        IRoomRepository roomRepository)
    {
        _bookingRepository = bookingRepository;
        _availabilityService = availabilityService;
        _pricingService = pricingService;
        _transactionManager = transactionManager;
        _roomRepository = roomRepository;
    }

    public async Task ModifyAsync(int bookingId, int userId, ModifyBookingRequestDto request)
    {
        if (bookingId <= 0)
        {
            throw new BadRequestException("Invalid booking ID.");
        }

        if (userId <= 0)
        {
            throw new BadRequestException("Invalid user ID.");
        }

        await _transactionManager.ExecuteSerializableAsync(async () =>
        {
            var booking = await _bookingRepository.GetByIdForUserAsync(bookingId, userId);

            if (booking == null)
            {
                throw new NotFoundException("Booking not found.");
            }

            if (DateTime.UtcNow >= booking.CheckIn)
            {
                throw new ConflictException("Booking cannot be modified after the stay has started.");
            }

            if (booking.BookingStatus == BookingStatus.Cancelled)
            {
                throw new ConflictException("Cancelled bookings cannot be modified.");
            }

            if (request.RoomId <= 0)
            {
                throw new BadRequestException("Invalid room ID.");
            }

            if (request.CheckIn == default || request.CheckOut == default)
            {
                throw new BadRequestException("Check-in and check-out dates are required.");
            }

            if (request.CheckOut <= request.CheckIn)
            {
                throw new BadRequestException("Check-out date must be after check-in date.");
            }

            if (request.Adults < 1)
            {
                throw new BadRequestException("At least one adult is required.");
            }

            if (request.Children < 0)
            {
                throw new BadRequestException("Children count cannot be negative.");
            }

            if (request.SpecialRequests?.Length > 1000)
            {
                throw new BadRequestException("Special requests cannot exceed 1000 characters.");
            }

            if (booking.Invoice == null)
            {
                throw new InvalidOperationException("Booking invoice was not loaded.");
            }

            var requestedRoom = await _roomRepository.GetRoomByIdAsync(request.RoomId);

            if (requestedRoom == null)
            {
                throw new NotFoundException("Room not found.");
            }

            if (!requestedRoom.IsActive || !requestedRoom.IsOperationallyAvailable)
            {
                throw new ConflictException("The selected room is not available for booking.");
            }

            if (request.Adults > requestedRoom.AdultsCapacity)
            {
                throw new BadRequestException("The number of adults exceeds the room capacity.");
            }

            if (request.Children > requestedRoom.ChildCapacity)
            {
                throw new BadRequestException("The number of children exceeds the room capacity.");
            }

            if (requestedRoom.HotelId != booking.Invoice.HotelId)
            {
                throw new BadRequestException("The booking can only be changed to a room in the same hotel.");
            }

            var roomOrDatesChanged = booking.RoomId != request.RoomId || booking.CheckIn != request.CheckIn || booking.CheckOut != request.CheckOut;

            if (roomOrDatesChanged)
            {
                var isAvailable = await _availabilityService.IsRoomAvailableAsync(request.RoomId, request.CheckIn, request.CheckOut, booking.BookingId);

                if (!isAvailable)
                {
                    throw new ConflictException("The selected room is not available for the requested dates.");
                }

                var price = await _pricingService.CalculatePriceAsync( request.RoomId, request.CheckIn, request.CheckOut, DateTime.UtcNow);

                booking.Modify(
                    request.RoomId,
                    request.CheckIn,
                    request.CheckOut,
                    request.Adults,
                    request.Children,
                    price.PricePerNight,
                    price.OriginalTotalPrice,
                    price.DiscountPercentage,
                    price.DiscountAmount,
                    price.TotalPrice,
                    request.SpecialRequests);
            }
            else
            {
                booking.Modify(
                    request.RoomId,
                    request.CheckIn,
                    request.CheckOut,
                    request.Adults,
                    request.Children,
                    booking.PricePerNight,
                    booking.OriginalTotalPrice,
                    booking.DiscountPercentage,
                    booking.DiscountAmount,
                    booking.TotalPrice,
                    request.SpecialRequests);
            }

            var updatedInvoiceTotal = booking.Invoice.Bookings.Sum(invoiceBooking => invoiceBooking.TotalPrice);
            booking.Invoice.UpdateTotal(updatedInvoiceTotal);
        });
    }
}
