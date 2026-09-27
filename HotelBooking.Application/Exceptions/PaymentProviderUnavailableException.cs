namespace HotelBooking.Application.Exceptions;

public sealed class PaymentProviderUnavailableException : Exception
{
    public PaymentProviderUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
