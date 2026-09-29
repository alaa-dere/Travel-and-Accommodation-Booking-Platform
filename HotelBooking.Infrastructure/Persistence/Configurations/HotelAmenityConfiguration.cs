using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class HotelAmenityConfiguration : IEntityTypeConfiguration<HotelAmenity>
{
    public void Configure(EntityTypeBuilder<HotelAmenity> builder)
    {
        builder.HasKey(hotelAmenity => new { hotelAmenity.HotelId, hotelAmenity.AmenityId });
        builder.HasOne(hotelAmenity => hotelAmenity.Hotel).WithMany(hotel => hotel.HotelAmenities).HasForeignKey(hotelAmenity => hotelAmenity.HotelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(hotelAmenity => hotelAmenity.Amenity).WithMany(amenity => amenity.HotelAmenities).HasForeignKey(hotelAmenity => hotelAmenity.AmenityId).OnDelete(DeleteBehavior.Restrict);
    }
}