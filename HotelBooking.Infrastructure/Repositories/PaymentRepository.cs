using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;

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
}