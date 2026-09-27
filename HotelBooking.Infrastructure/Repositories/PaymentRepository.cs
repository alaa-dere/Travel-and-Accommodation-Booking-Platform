using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public PaymentRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Payment payment)
    {
        await _dbContext.Payments.AddAsync(payment);
    }

    public Task<Payment?> GetByProviderPaymentIdAsync(string providerPaymentId)
    {
        return _dbContext.Payments
            .Include(payment => payment.Invoice)
                .ThenInclude(invoice => invoice!.Hotel)
            .Include(payment => payment.Invoice)
                .ThenInclude(invoice => invoice!.Bookings)
                    .ThenInclude(booking => booking.Room)
            .SingleOrDefaultAsync(payment => payment.ProviderPaymentId == providerPaymentId);
    }
}
