using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IInvoiceRepository
{
    Task AddAsync(Invoice invoice);
}