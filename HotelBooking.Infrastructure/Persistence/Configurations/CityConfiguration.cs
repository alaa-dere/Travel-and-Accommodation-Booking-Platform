using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
      builder.HasKey(c => c.CityId);
      builder.Property(c => c.Name).IsRequired().HasMaxLength(50);
      builder.Property(c => c.Country).IsRequired().HasMaxLength(50);
      builder.Property(c => c.PostOffice).IsRequired().HasMaxLength(50);
      builder.Property(c => c.ThumbnailUrl).HasMaxLength(500);
      builder.HasIndex(c => new { c.Name, c.Country, c.PostOffice }).IsUnique();
    }
}

