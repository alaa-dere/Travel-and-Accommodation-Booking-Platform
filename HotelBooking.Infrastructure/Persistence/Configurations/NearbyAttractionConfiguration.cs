using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class NearbyAttractionConfiguration : IEntityTypeConfiguration<NearbyAttraction>
{
    public void Configure(EntityTypeBuilder<NearbyAttraction> builder)
    {
        builder.HasKey(attraction => attraction.NearbyAttractionId);
        builder.HasOne(attraction => attraction.Hotel).WithMany(hotel => hotel.NearbyAttractions).HasForeignKey(attraction => attraction.HotelId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.Property(attraction => attraction.Name).IsRequired().HasMaxLength(100);
        builder.Property(attraction => attraction.Description).HasMaxLength(500);
    }
}