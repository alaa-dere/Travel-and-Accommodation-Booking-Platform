using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration :  IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.ReviewId);
        builder.HasOne(r => r.Booking).WithOne(b => b.Review).HasForeignKey<Review>(r => r.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(b => b.BookingId).IsUnique();
    }
}