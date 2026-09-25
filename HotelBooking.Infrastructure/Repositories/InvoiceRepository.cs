using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public InvoiceRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Invoice invoice)
    {
        await _dbContext.Invoices.AddAsync(invoice);
    }
    
    public async Task<Invoice?> GetByIdForUserAsync(int invoiceId, int userId)
    {
        return await _dbContext.Invoices.AsNoTracking()
            .Include(invoice => invoice.Hotel)
            .Include(invoice => invoice.Payment)
            .Include(invoice => invoice.Bookings)
            .ThenInclude(booking => booking.Room)
            .FirstOrDefaultAsync(invoice => invoice.InvoiceId == invoiceId && invoice.UserId == userId);
    }
}