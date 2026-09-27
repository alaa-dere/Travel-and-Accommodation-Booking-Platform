namespace HotelBooking.Application.Exceptions;

public sealed class InvalidPaymentWebhookException : Exception
{
    public InvalidPaymentWebhookException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
