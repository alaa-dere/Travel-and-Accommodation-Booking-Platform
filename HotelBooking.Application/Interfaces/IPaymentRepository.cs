using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment);
    Task<Payment?> GetByIdAsync(int paymentId);
    Task<Payment?> GetByProviderPaymentIdAsync(string providerPaymentId);
    Task<Payment?> GetByProviderRefundIdAsync(string providerRefundId);
}
