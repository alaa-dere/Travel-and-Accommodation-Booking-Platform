using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
    public void Configure(EntityTypeBuilder<Hotel> builder)
    {
        builder.HasKey(hotel => hotel.HotelId);
        builder.HasOne(hotel => hotel.City).WithMany(c => c.Hotels).HasForeignKey(hotel => hotel.CityId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.Property(hotel => hotel.Name).IsRequired().HasMaxLength(50);
        builder.Property(hotel => hotel.OwnerName).IsRequired().HasMaxLength(50);
        builder.Property(hotel => hotel.Address).IsRequired().HasMaxLength(200);
    }
}
