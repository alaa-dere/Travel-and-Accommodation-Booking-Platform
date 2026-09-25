namespace HotelBooking.Application.Interfaces;

public interface IBookingTransactionManager
{
    Task ExecuteSerializableAsync(Func<Task> operation);
}