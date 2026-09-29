using Microsoft.EntityFrameworkCore;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.UserId);
        builder.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.LastName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.Username).HasMaxLength(100).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(100).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(user => user.Role).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.HasIndex(user => user.Username).IsUnique();
        builder.HasIndex(user => user.Email).IsUnique();
    }
}