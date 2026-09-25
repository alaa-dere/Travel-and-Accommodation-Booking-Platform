using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Bookings;

public class CreateBookingsService : ICreateBookingsService
{
    private readonly ICartRepository _cartRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IBookingAvailabilityService _availabilityService;
    private readonly IBookingPricingService _pricingService;
    private readonly IBookingTransactionManager _transactionManager;

    public CreateBookingsService(
        ICartRepository cartRepository,
        IBookingRepository bookingRepository,
        IInvoiceRepository invoiceRepository,
        IBookingAvailabilityService availabilityService,
        IBookingPricingService pricingService,
        IBookingTransactionManager transactionManager)
    {
        _cartRepository = cartRepository;
        _bookingRepository = bookingRepository;
        _invoiceRepository = invoiceRepository;
        _availabilityService = availabilityService;
        _pricingService = pricingService;
        _transactionManager = transactionManager;
    }

   public async Task<BookingCreationResultDto> CreateBookingsAsync(int userId)
{
    var bookingCount = 0;
    var invoiceCount = 0;

    await _transactionManager.ExecuteSerializableAsync(async () =>
    {
        var cartItems = await _cartRepository.GetByUserIdAsync(userId);
        if (cartItems.Count == 0)
        {
            throw new BadRequestException("Cart is empty.");
        }

        var bookingCreationTime = DateTime.UtcNow;
        var itemsByHotel = cartItems.GroupBy(item => item.Room!.HotelId);

        foreach (var hotelGroup in itemsByHotel)
        {
            var pricedBookings = new List<Booking>();
            foreach (var item in hotelGroup)
            {
                var isAvailable =
                    await _availabilityService.IsRoomAvailableAsync(item.RoomId, item.CheckIn, item.CheckOut);

                if (!isAvailable)
                {
                    throw new ConflictException($"Room {item.RoomId} is no longer available.");
                }

                var price = await _pricingService.CalculatePriceAsync(item.RoomId, item.CheckIn, item.CheckOut, bookingCreationTime);

                var booking = new Booking(
                    userId,
                    item.RoomId,
                    item.CheckIn,
                    item.CheckOut,
                    item.Adults,
                    item.Children,
                    price.PricePerNight,
                    price.OriginalTotalPrice,
                    price.DiscountPercentage,
                    price.DiscountAmount,
                    price.TotalPrice);
                pricedBookings.Add(booking);
            }

            var invoiceTotal = pricedBookings.Sum(booking => booking.TotalPrice);
            var invoice = new Invoice(userId, hotelGroup.Key, invoiceTotal);
            await _invoiceRepository.AddAsync(invoice);

            foreach (var booking in pricedBookings)
            {
                booking.Invoice = invoice;
                await _bookingRepository.AddAsync(booking);
                bookingCount++;
            }
            invoiceCount++;
        }

        _cartRepository.DeleteRange(cartItems);
    });

    return new BookingCreationResultDto
    {
        BookingCount = bookingCount,
        InvoiceCount = invoiceCount
    };
}
}