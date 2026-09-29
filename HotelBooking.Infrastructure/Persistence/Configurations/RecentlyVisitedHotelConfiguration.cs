using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class RecentlyVisitedHotelConfiguration 
    : IEntityTypeConfiguration<RecentlyVisitedHotel>
{
    public void Configure(EntityTypeBuilder<RecentlyVisitedHotel> builder)
    {
        builder.HasKey(visit => visit.RecentlyVisitedHotelId);
        builder.HasOne(visit => visit.User).WithMany(user => user.RecentlyVisitedHotels).HasForeignKey(visit => visit.UserId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(visit => visit.Hotel).WithMany(hotel => hotel.RecentlyVisitedHotels).HasForeignKey(visit => visit.HotelId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(visit => new { visit.UserId, visit.HotelId }).IsUnique();
        builder.Property(visit => visit.VisitedAt).IsRequired();
    }
}