using HotelBooking.Domain.ValueObjects;

namespace HotelBooking.Domain.Entities;

internal static class BookingTestFactory
{
    internal static Booking Create(
        int userId,
        int roomId,
        DateTime checkIn,
        DateTime checkOut,
        int adults,
        int children,
        decimal pricePerNight,
        decimal originalTotalPrice,
        int discountPercentage,
        decimal discountAmount,
        decimal totalPrice,
        string? specialRequests,
        DateTime createdAt,
        DateTime pendingExpiresAt)
    {
        return new Booking(
            userId,
            new BookingStay(roomId, checkIn, checkOut, adults, children),
            new BookingPrice(
                pricePerNight,
                originalTotalPrice,
                discountPercentage,
                discountAmount,
                totalPrice),
            new PendingBookingWindow(createdAt, pendingExpiresAt),
            specialRequests);
    }
}
