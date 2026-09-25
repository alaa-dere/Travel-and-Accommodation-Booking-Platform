using HotelBooking.Application.Invoices.Dtos;

namespace HotelBooking.Application.Interfaces;

public interface IInvoicePdfGenerator
{
    byte[] Generate(InvoicePdfDto invoice);
}