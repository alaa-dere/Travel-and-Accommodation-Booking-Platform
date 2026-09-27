namespace HotelBooking.Application.Payments;

public interface IPaymentWebhookService
{
    Task HandleAsync(string payload, string signature);
}
