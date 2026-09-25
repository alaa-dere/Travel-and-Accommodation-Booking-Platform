using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Invoices.Dtos;

namespace HotelBooking.Application.Invoices;

public class GetInvoiceForPdfService : IGetInvoiceForPdfService
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoiceForPdfService(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<InvoicePdfDto> GetAsync(int invoiceId, int userId)
    {
        if (invoiceId <= 0)
        {
            throw new BadRequestException("Invalid invoice ID.");
        }

        if (userId <= 0)
        {
            throw new BadRequestException("Invalid user ID.");
        }

        var invoice =
            await _invoiceRepository.GetByIdForUserAsync(invoiceId, userId);

        if (invoice == null)
        {
            throw new NotFoundException("Invoice not found.");
        }

        return new InvoicePdfDto
        {
            InvoiceId = invoice.InvoiceId,
            HotelName = invoice.Hotel?.Name ?? string.Empty,
            TotalAmount = invoice.TotalAmount,
            PaymentStatus = invoice.Payment?.Status ?? throw new InvalidOperationException("Invoice payment information is missing."),
            CreatedAt = invoice.CreatedAt,

            Bookings = invoice.Bookings.Select(booking =>
                new InvoiceBookingDto
                {
                    BookingId = booking.BookingId,
                    RoomNumber = booking.Room?.RoomNumber ?? string.Empty,
                    CheckIn = booking.CheckIn,
                    CheckOut = booking.CheckOut,
                    PricePerNight = booking.PricePerNight,
                    OriginalTotalPrice = booking.OriginalTotalPrice,
                    DiscountPercentage = booking.DiscountPercentage,
                    DiscountAmount = booking.DiscountAmount,
                    TotalPrice = booking.TotalPrice
                }).ToList()
        };
    }
}