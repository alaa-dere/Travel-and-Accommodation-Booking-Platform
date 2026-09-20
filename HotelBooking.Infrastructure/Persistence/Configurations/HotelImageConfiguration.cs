using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class HotelImageConfiguration : IEntityTypeConfiguration<HotelImage>
{
    public void Configure(EntityTypeBuilder<HotelImage> builder)
    {
        builder.HasKey(h => h.HotelImageId);
        builder.HasOne(i => i.Hotel).WithMany(h => h.HotelImages).HasForeignKey(h => h.HotelId).IsRequired();
        builder.Property(i => i.ImageUrl).IsRequired().HasMaxLength(500);
    }
}