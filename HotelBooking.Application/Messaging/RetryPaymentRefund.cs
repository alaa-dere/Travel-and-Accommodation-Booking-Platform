namespace HotelBooking.Application.Messaging;

public sealed record RetryPaymentRefund(int PaymentId, string ProviderPaymentId);
