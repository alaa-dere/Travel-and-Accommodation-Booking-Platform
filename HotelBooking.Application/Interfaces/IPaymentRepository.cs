using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment);
    Task<Payment?> GetByProviderPaymentIdAsync(string providerPaymentId);
}
