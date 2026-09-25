using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Checkout.Dtos;
using HotelBooking.Application.Checkout.Dtos.Confirmation;
using HotelBooking.Application.Emails;
using HotelBooking.Application.Emails.Dtos;
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
    private readonly IBookingConfirmationEmailService _emailService;
    private readonly IUserRepository _userRepository;

    public CreateBookingsService(
        ICartRepository cartRepository,
        IBookingRepository bookingRepository,
        IInvoiceRepository invoiceRepository,
        IBookingAvailabilityService availabilityService,
        IBookingPricingService pricingService,
        IBookingTransactionManager transactionManager,
        IPaymentService paymentService,
        IBookingConfirmationEmailService emailService,
        IUserRepository userRepository)
    {
        _cartRepository = cartRepository;
        _bookingRepository = bookingRepository;
        _invoiceRepository = invoiceRepository;
        _availabilityService = availabilityService;
        _pricingService = pricingService;
        _transactionManager = transactionManager;
        _paymentService = paymentService;
        _emailService = emailService;
        _userRepository = userRepository;
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

        var customerEmail = await _userRepository.GetEmailByIdAsync(userId);

        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            throw new NotFoundException("Customer email was not found.");
        }

        var bookingCount = 0;
        var invoiceCount = 0;
        var paymentResults = new List<CheckoutPaymentResultDto>();
        var confirmations = new List<BookingConfirmationDto>();
        var confirmationData = new List<(Invoice Invoice, List<Booking> Bookings, Payment Payment, string HotelName)>();
        var emailConfirmationData = new List<(Invoice Invoice, List<Booking> Bookings, Payment Payment, string HotelName)>();

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

                    booking.Room = item.Room;

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

                if (payment.Status == PaymentStatus.Paid)
                {
                    var hotelName = hotelGroup.First().Room!.Hotel!.Name;

                    confirmationData.Add((invoice, pricedBookings, payment, hotelName));
                    emailConfirmationData.Add((invoice, pricedBookings, payment, hotelName));
                }

                paymentResults.Add(new CheckoutPaymentResultDto { Amount = payment.Amount, Status = payment.Status });
                
                invoiceCount++;
            }

            var allPaymentsSucceeded = paymentResults.All(payment => payment.Status == PaymentStatus.Paid);

            if (allPaymentsSucceeded)
            {
                _cartRepository.DeleteRange(cartItems);
            }
        });
        
        foreach (var data in emailConfirmationData)
        {
            var emailConfirmation = new BookingConfirmationEmailDto
                {
                    CustomerEmail = customerEmail,
                    InvoiceId = data.Invoice.InvoiceId,
                    HotelName = data.HotelName,
                    InvoiceTotal = data.Invoice.TotalAmount,
                    PaymentStatus = data.Payment.Status,

                    Rooms = data.Bookings.Select(booking => new BookingConfirmationEmailRoomDto
                            {
                                BookingId = booking.BookingId,
                                RoomNumber = booking.Room?.RoomNumber ?? string.Empty,
                                CheckIn = booking.CheckIn,
                                CheckOut = booking.CheckOut,
                                TotalPrice = booking.TotalPrice
                            }).ToList()
                };

            try
            {
                await _emailService.SendAsync(emailConfirmation);
            }
            catch
            {
                // The email infrastructure already logs the failure.
                // Do not fail an already committed checkout.
            }
        }

        foreach (var data in confirmationData)
        {
            confirmations.Add(new BookingConfirmationDto
                {
                    ConfirmationId = data.Invoice.InvoiceId,
                    HotelId = data.Invoice.HotelId,
                    HotelName = data.HotelName,
                    TotalAmount = data.Invoice.TotalAmount,
                    PaymentStatus = data.Payment.Status,

                    Rooms = data.Bookings
                        .Select(booking => new BookingConfirmationRoomDto
                            {
                                BookingId = booking.BookingId,
                                RoomId = booking.RoomId,
                                RoomNumber = booking.Room?.RoomNumber ?? string.Empty,
                                CheckIn = booking.CheckIn,
                                CheckOut = booking.CheckOut,
                                TotalAmount = booking.TotalPrice
                            }).ToList()
                });
        }

        return new BookingCreationResultDto
        {
            BookingCount = bookingCount,
            InvoiceCount = invoiceCount,
            Payments = paymentResults,
            Confirmations = confirmations
        };
    }
}