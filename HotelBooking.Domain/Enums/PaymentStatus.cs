namespace HotelBooking.Domain.Enums;

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    RequiresAction = 4,
    Cancelled = 5,
    Refunded = 6
}
