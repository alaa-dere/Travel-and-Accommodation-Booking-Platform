using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.HasKey(amenity => amenity.AmenityId);
        builder.Property(amenity => amenity.Name).IsRequired().HasMaxLength(100);
        builder.Property(amenity => amenity.Description).HasMaxLength(500);
    }
}