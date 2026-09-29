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

    public Task<Payment?> GetByIdAsync(int paymentId) => QueryForPaymentProcessing()
        .SingleOrDefaultAsync(payment => payment.PaymentId == paymentId);

    public Task<Payment?> GetByProviderPaymentIdAsync(string providerPaymentId)
    {
        return QueryForPaymentProcessing()
            .SingleOrDefaultAsync(payment => payment.ProviderPaymentId == providerPaymentId);
    }

    private IQueryable<Payment> QueryForPaymentProcessing()
    {
        return _dbContext.Payments
            .Include(payment => payment.Invoice)
                .ThenInclude(invoice => invoice!.Hotel)
            .Include(payment => payment.Invoice)
                .ThenInclude(invoice => invoice!.Bookings)
                    .ThenInclude(booking => booking.Room);
    }

    public Task<Payment?> GetByProviderRefundIdAsync(string providerRefundId)
    {
        return _dbContext.Payments
            .SingleOrDefaultAsync(payment => payment.ProviderRefundId == providerRefundId);
    }
}
