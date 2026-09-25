using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(cartItem => cartItem.CartItemId);
        builder.HasOne(cartItem => cartItem.User).WithMany(user => user.CartItems).HasForeignKey(cartItem => cartItem.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(cartItem => cartItem.Room).WithMany(room => room.CartItems) .HasForeignKey(cartItem => cartItem.RoomId).OnDelete(DeleteBehavior.Restrict);
    }
}