using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class RoomImageConfiguration: IEntityTypeConfiguration<RoomImage>
{
    public void Configure(EntityTypeBuilder<RoomImage> builder)
    {
        builder.HasKey(i => i.RoomImageId);
        builder.HasOne(i => i.Room).WithMany(r => r.RoomImages).HasForeignKey(i => i.RoomId).IsRequired();
        builder.Property(i => i.ImageUrl).IsRequired().HasMaxLength(500);
        builder.HasIndex(image => new { image.RoomId, image.ImageUrl }).IsUnique();
        builder.HasIndex(image => new { image.RoomId, image.DisplayOrder }).IsUnique();
    }
}
