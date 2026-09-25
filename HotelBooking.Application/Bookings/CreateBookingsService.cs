using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Checkout.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Payments;
using HotelBooking.Application.Payments.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;

namespace HotelBooking.Application.Bookings;

public class CreateBookingsService : ICreateBookingsService
{
    private readonly ICartRepository _cartRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IBookingAvailabilityService _availabilityService;
    private readonly IBookingPricingService _pricingService;
    private readonly IBookingTransactionManager _transactionManager;
    private readonly IPaymentService _paymentService;

    public CreateBookingsService(
        ICartRepository cartRepository,
        IBookingRepository bookingRepository,
        IInvoiceRepository invoiceRepository,
        IBookingAvailabilityService availabilityService,
        IBookingPricingService pricingService,
        IBookingTransactionManager transactionManager,
        IPaymentService paymentService)
    {
        _cartRepository = cartRepository;
        _bookingRepository = bookingRepository;
        _invoiceRepository = invoiceRepository;
        _availabilityService = availabilityService;
        _pricingService = pricingService;
        _transactionManager = transactionManager;
        _paymentService = paymentService;
    }

    public async Task<BookingCreationResultDto> CreateBookingsAsync(int userId, string? specialRequests, PaymentInformationDto paymentInformation)
    {
        if (userId <= 0)
        {
            throw new BadRequestException("Invalid user ID.");
        }

        if (paymentInformation == null)
        {
            throw new BadRequestException("Payment information is required.");
        }

        if (specialRequests?.Length > 1000)
        {
            throw new BadRequestException("Special requests cannot exceed 1000 characters.");
        }

        var bookingCount = 0;
        var invoiceCount = 0;
        var paymentResults = new List<CheckoutPaymentResultDto>();
        
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
                    var isAvailable = await _availabilityService.IsRoomAvailableAsync(item.RoomId, item.CheckIn, item.CheckOut);

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
                        price.TotalPrice,
                        specialRequests);

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

                var payment = await _paymentService.ProcessPaymentAsync(invoice, paymentInformation);

                if (payment.Status == PaymentStatus.Failed)
                {
                    foreach (var booking in pricedBookings)
                    {
                        booking.Cancel();
                    }
                }

                paymentResults.Add(new CheckoutPaymentResultDto
                {
                    Amount = payment.Amount,
                    Status = payment.Status
                });

                invoiceCount++;
            }

            var allPaymentsSucceeded = paymentResults.All(payment => payment.Status == PaymentStatus.Paid);

            if (allPaymentsSucceeded)
            {
                _cartRepository.DeleteRange(cartItems);
            }
        });

        return new BookingCreationResultDto
        {
            BookingCount = bookingCount,
            InvoiceCount = invoiceCount,
            Payments = paymentResults
        };
    }
}