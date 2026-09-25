using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.BookingId);
        builder.HasOne(b => b.Room).WithMany(r => r.Bookings).HasForeignKey(b => b.RoomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.User).WithMany(u => u.Bookings).HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(b => b.PricePerNight).HasColumnType("decimal(18,2)");
        builder.Property(b => b.TotalPrice).HasColumnType("decimal(18,2)");
        builder.Property(b => b.OriginalTotalPrice).HasColumnType("decimal(18,2)");
        builder.Property(b => b.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.HasOne(booking => booking.Invoice).WithMany(invoice => invoice.Bookings).HasForeignKey(booking => booking.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(booking => new { booking.RoomId, booking.CheckIn, booking.CheckOut
});
    }
}