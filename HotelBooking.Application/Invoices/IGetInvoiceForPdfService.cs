using HotelBooking.Application.Invoices.Dtos;

namespace HotelBooking.Application.Invoices;

public interface IGetInvoiceForPdfService
{
    Task<InvoicePdfDto> GetAsync(int invoiceId, int userId);
}