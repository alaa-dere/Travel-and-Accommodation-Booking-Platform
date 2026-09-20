using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasKey( r => r.RoomId );
        builder.HasOne( r => r.Hotel ).WithMany(hotel => hotel.Rooms).HasForeignKey( r => r.HotelId ).IsRequired();
        builder.Property( r => r.RoomNumber ).IsRequired().HasMaxLength(50);
        builder.HasIndex( r => new {r.RoomNumber,r.HotelId} ).IsUnique();
    }
}