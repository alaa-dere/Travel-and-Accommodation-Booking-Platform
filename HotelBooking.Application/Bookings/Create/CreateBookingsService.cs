using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Checkout.Dtos;
using HotelBooking.Application.Checkout.Dtos.Confirmation;
using HotelBooking.Application.Common.Settings;
using HotelBooking.Application.Emails;
using HotelBooking.Application.Emails.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Payments;
using HotelBooking.Application.Payments.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;
using HotelBooking.Domain.ValueObjects;

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
    private readonly TimeProvider _timeProvider;
    private readonly BookingSettings _bookingSettings;

    public CreateBookingsService(
        ICartRepository cartRepository,
        IBookingRepository bookingRepository,
        IInvoiceRepository invoiceRepository,
        IBookingAvailabilityService availabilityService,
        IBookingPricingService pricingService,
        IBookingTransactionManager transactionManager,
        IPaymentService paymentService,
        IBookingConfirmationEmailService emailService,
        IUserRepository userRepository,
        TimeProvider timeProvider,
        BookingSettings bookingSettings)
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
        _timeProvider = timeProvider;
        _bookingSettings = bookingSettings;
    }

    public async Task<BookingCreationResultDto> CreateBookingsAsync(int userId, string? specialRequests, PaymentInformationDto paymentInformation)
    {
        ValidateRequest(userId, specialRequests, paymentInformation);

        var customerEmail = await GetCustomerEmailAsync(userId);
        List<CartItem> cartItems = [];
        PendingCheckout? checkout = null;

        await _transactionManager.ExecuteSerializableAsync(async () =>
        {
            cartItems = await _cartRepository.GetByUserIdAsync(userId);
            ValidateCart(cartItems);
            checkout = await CreatePendingCheckoutAsync(userId, cartItems, specialRequests);
        });

        await _paymentService.ProcessPaymentAsync(checkout!.Payment, paymentInformation);

        await _transactionManager.ExecuteSerializableAsync(() =>
        {
            ApplyPaymentResult(checkout, cartItems);
            return Task.CompletedTask;
        });

        if (checkout.Payment.Status == PaymentStatus.Paid)
        {
            await TrySendConfirmationEmailAsync(customerEmail, checkout);
        }
        return BuildResult(checkout);
    }

    private async Task<PendingCheckout> CreatePendingCheckoutAsync(int userId, List<CartItem> cartItems, string? specialRequests)
    {
        var createdAt = _timeProvider.GetUtcNow().UtcDateTime;
        var pendingWindow = new PendingBookingWindow(createdAt, createdAt.AddMinutes(_bookingSettings.PendingExpirationMinutes));
        var rooms = new List<(Booking Booking, string RoomNumber)>();

        foreach (var item in cartItems)
        {
            var booking = await CreateBookingAsync(userId, item, pendingWindow, specialRequests);
            rooms.Add((booking, item.Room!.RoomNumber));
        }

        var firstRoom = cartItems[0].Room!;
        var invoice = new Invoice(userId, firstRoom.HotelId, rooms.Sum(room => room.Booking.TotalPrice));

        await _invoiceRepository.AddAsync(invoice);

        foreach (var room in rooms)
        {
            room.Booking.AssignToInvoice(invoice);
            await _bookingRepository.AddAsync(room.Booking);
        }

        var payment = await _paymentService.CreatePendingPaymentAsync(invoice);
        return new PendingCheckout(invoice, rooms, payment, firstRoom.Hotel!.Name);
    }

    private async Task<Booking> CreateBookingAsync(int userId, CartItem item, PendingBookingWindow pendingWindow, string? specialRequests)
    {
        var isAvailable = await _availabilityService.IsRoomAvailableAsync(item.RoomId, item.CheckIn, item.CheckOut);

        if (!isAvailable)
        {
            throw new ConflictException($"Room {item.RoomId} is no longer available.");
        }
        var price = await _pricingService.CalculatePriceAsync(item.RoomId, item.CheckIn, item.CheckOut, pendingWindow.CreatedAt);

        return new Booking(userId,
            new BookingStay(item.RoomId, item.CheckIn, item.CheckOut, item.Adults, item.Children),
            new BookingPrice(price.PricePerNight, price.OriginalTotalPrice, price.DiscountPercentage, price.DiscountAmount, price.TotalPrice),
            pendingWindow, specialRequests);
    }

    private void ApplyPaymentResult(PendingCheckout checkout, List<CartItem> cartItems)
    {
        var processedAt = _timeProvider.GetUtcNow().UtcDateTime;

        if (checkout.Payment.Status is PaymentStatus.Failed or PaymentStatus.Cancelled)
        {
            foreach (var room in checkout.Rooms)
                room.Booking.Cancel(processedAt);

            return;
        }

        if (checkout.Payment.Status != PaymentStatus.Paid)
            return;

        foreach (var room in checkout.Rooms)
            room.Booking.Confirm(processedAt);

        _cartRepository.DeleteRange(cartItems);
    }

    private async Task<string> GetCustomerEmailAsync(int userId)
    {
        var email = await _userRepository.GetEmailByIdAsync(userId);
        return string.IsNullOrWhiteSpace(email) ? throw new NotFoundException("Customer email was not found.") : email;
    }

    private async Task TrySendConfirmationEmailAsync(string customerEmail, PendingCheckout checkout)
    {
        try
        {
            await _emailService.SendAsync(new BookingConfirmationEmailDto
            {
                CustomerEmail = customerEmail,
                InvoiceId = checkout.Invoice.InvoiceId,
                HotelName = checkout.HotelName,
                InvoiceTotal = checkout.Invoice.TotalAmount,
                PaymentStatus = checkout.Payment.Status,
                Rooms = checkout.Rooms.Select(room => new BookingConfirmationEmailRoomDto
                {
                    BookingId = room.Booking.BookingId,
                    RoomNumber = room.RoomNumber,
                    CheckIn = room.Booking.CheckIn,
                    CheckOut = room.Booking.CheckOut,
                    TotalPrice = room.Booking.TotalPrice
                }).ToList()
            });
        }
        catch
        {
        }
    }

    private static BookingCreationResultDto BuildResult(PendingCheckout checkout)
    {
        var payment = new CheckoutPaymentResultDto
        {
            PaymentId = checkout.Payment.PaymentId,
            Amount = checkout.Payment.Amount,
            Status = checkout.Payment.Status,
            ProviderPaymentId = checkout.Payment.ProviderPaymentId,
            ClientSecret = checkout.Payment.ClientSecret
        };

        var confirmations = checkout.Payment.Status == PaymentStatus.Paid
            ? new List<BookingConfirmationDto> { BuildConfirmation(checkout) } : [];

        return new BookingCreationResultDto
        {
            BookingCount = checkout.Rooms.Count,
            InvoiceCount = 1,
            Payments = [payment],
            Confirmations = confirmations
        };
    }

    private static BookingConfirmationDto BuildConfirmation(PendingCheckout checkout) => new()
    {
        ConfirmationId = checkout.Invoice.InvoiceId,
        HotelId = checkout.Invoice.HotelId,
        HotelName = checkout.HotelName,
        TotalAmount = checkout.Invoice.TotalAmount,
        PaymentStatus = checkout.Payment.Status,
        Rooms = checkout.Rooms.Select(room => new BookingConfirmationRoomDto
        {
            BookingId = room.Booking.BookingId,
            RoomId = room.Booking.RoomId,
            RoomNumber = room.RoomNumber,
            CheckIn = room.Booking.CheckIn,
            CheckOut = room.Booking.CheckOut,
            TotalAmount = room.Booking.TotalPrice
        }).ToList()
    };

    private static void ValidateRequest(int userId, string? specialRequests, PaymentInformationDto paymentInformation)
    {
        if (userId <= 0)
        {
            throw new BadRequestException("Invalid user ID.");
        }
        if (paymentInformation is null)
        {
            throw new BadRequestException("Payment information is required.");
        }
        if (specialRequests?.Length > 1000)
        {
            throw new BadRequestException("Special requests cannot exceed 1000 characters.");
        }
        
    }

    private static void ValidateCart(List<CartItem> cartItems)
    {
        if (cartItems.Count == 0)
        {
            throw new BadRequestException("Cart is empty.");
        }
        var hotelId = cartItems[0].Room!.HotelId;
        if (cartItems.Any(item => item.Room!.HotelId != hotelId))
        {
            throw new ConflictException("Checkout can only process rooms from one hotel. Complete or clear the current cart before booking another hotel.");
        }
    }

    private sealed record PendingCheckout(Invoice Invoice, List<(Booking Booking, string RoomNumber)> Rooms, Payment Payment, string HotelName);
}
