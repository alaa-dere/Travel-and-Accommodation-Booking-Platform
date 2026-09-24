using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class PromotionConfiguration :IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.HasKey(p => p.PromotionId);
        builder.HasOne(p => p.Hotel).WithMany(h => h.Promotions).HasForeignKey(p => p.HotelId).OnDelete(DeleteBehavior.Restrict);
    }
}