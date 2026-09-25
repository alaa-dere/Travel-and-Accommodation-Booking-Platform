using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(payment => payment.PaymentId);
        builder.Property(payment => payment.Amount).HasColumnType("decimal(18,2)");
        builder.HasOne(payment => payment.Invoice).WithOne(invoice => invoice.Payment).HasForeignKey<Payment>(payment => payment.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(payment => payment.InvoiceId).IsUnique();
    }
}