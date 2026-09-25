using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(invoice => invoice.InvoiceId);
        builder.Property(invoice => invoice.TotalAmount).HasColumnType("decimal(18,2)");
        builder.HasOne(invoice => invoice.User).WithMany(user => user.Invoices).HasForeignKey(invoice => invoice.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(invoice => invoice.Hotel).WithMany(hotel => hotel.Invoices).HasForeignKey(invoice => invoice.HotelId).OnDelete(DeleteBehavior.Restrict);
    }
}