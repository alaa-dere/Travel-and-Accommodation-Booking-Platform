namespace HotelBooking.Domain.Entities;

internal static class BookingTestExtensions
{
    internal static void TransitionTo(this Booking booking, BookingStatus status)
    {
        switch (status)
        {
            case BookingStatus.Pending:
                return;
            case BookingStatus.Confirmed:
                booking.Confirm(booking.CreatedAt);
                return;
            case BookingStatus.Completed:
                booking.Confirm(booking.CreatedAt);
                booking.Complete(DateTime.SpecifyKind(booking.CheckOut.AddTicks(1), DateTimeKind.Utc));
                return;
            case BookingStatus.Cancelled:
                booking.Cancel(booking.CreatedAt);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }
    }
}
