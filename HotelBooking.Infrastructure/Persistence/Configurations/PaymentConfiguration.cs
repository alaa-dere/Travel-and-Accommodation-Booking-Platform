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
        builder.Property(payment => payment.Provider).HasMaxLength(30).IsRequired();
        builder.Property(payment => payment.ProviderPaymentId).HasMaxLength(255);
        builder.Property(payment => payment.Currency).HasMaxLength(3).IsRequired();
        builder.Property(payment => payment.ClientSecret).HasMaxLength(500);
        builder.Property(payment => payment.FailureCode).HasMaxLength(100);
        builder.Property(payment => payment.ProviderRefundId).HasMaxLength(255);
        builder.Property(payment => payment.RefundFailureCode).HasMaxLength(100);
        builder.HasOne(payment => payment.Invoice).WithOne(invoice => invoice.Payment).HasForeignKey<Payment>(payment => payment.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(payment => payment.InvoiceId).IsUnique();
        builder.HasIndex(payment => payment.ProviderPaymentId).IsUnique().HasFilter("[ProviderPaymentId] IS NOT NULL");
        builder.HasIndex(payment => payment.ProviderRefundId).IsUnique().HasFilter("[ProviderRefundId] IS NOT NULL");
    }
}
