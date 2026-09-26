using HotelBooking.Application.Cart;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Cart;

public class RemoveCartItemServiceTests
{
    private readonly Mock<ICartRepository> _cartRepositoryMock;
    private readonly RemoveCartItemService _service;

    public RemoveCartItemServiceTests()
    {
        _cartRepositoryMock = new Mock<ICartRepository>();
        _service = new RemoveCartItemService(_cartRepositoryMock.Object);
    }

    [Fact]
    public async Task RemoveAsync_WhenCartItemDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int cartItemId = 10;
        const int userId = 1;

        _cartRepositoryMock.Setup(repository => repository.GetByIdAsync(cartItemId)).ReturnsAsync((CartItem?)null);

        // Act
        var action = async () => await _service.RemoveAsync(cartItemId, userId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _cartRepositoryMock.Verify(repository => repository.Delete( It.IsAny<CartItem>()), Times.Never);
        _cartRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WhenCartItemBelongsToDifferentUser_ShouldThrowNotFoundException()
    {
        // Arrange
        const int cartItemId = 10;
        const int requestedUserId = 1;

        var cartItem = CreateCartItem(userId: 2, cartItemId: cartItemId);

        _cartRepositoryMock.Setup(repository => repository.GetByIdAsync(cartItemId)).ReturnsAsync(cartItem);

        // Act
        var action = async () => await _service.RemoveAsync(cartItemId, requestedUserId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _cartRepositoryMock.Verify(repository => repository.Delete(It.IsAny<CartItem>()), Times.Never);
        _cartRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WhenCartItemBelongsToUser_ShouldDeleteItem()
    {
        // Arrange
        const int cartItemId = 10;
        const int userId = 1;

        var cartItem = CreateCartItem(userId, cartItemId);

        _cartRepositoryMock.Setup(repository => repository.GetByIdAsync(cartItemId)).ReturnsAsync(cartItem);

        // Act
        await _service.RemoveAsync(cartItemId, userId);

        // Assert
        _cartRepositoryMock.Verify(repository => repository.Delete(cartItem), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WhenCartItemBelongsToUser_ShouldSaveChanges()
    {
        // Arrange
        const int cartItemId = 10;
        const int userId = 1;

        var cartItem = CreateCartItem(userId, cartItemId);

        _cartRepositoryMock.Setup(repository => repository.GetByIdAsync(cartItemId)).ReturnsAsync(cartItem);

        // Act
        await _service.RemoveAsync(cartItemId, userId);

        // Assert
        _cartRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRequestCorrectCartItemFromRepository()
    {
        // Arrange
        const int cartItemId = 10;
        const int userId = 1;

        var cartItem = CreateCartItem(userId, cartItemId);

        _cartRepositoryMock.Setup(repository => repository.GetByIdAsync(cartItemId)).ReturnsAsync(cartItem);

        // Act
        await _service.RemoveAsync(cartItemId, userId);

        // Assert
        _cartRepositoryMock.Verify(repository => repository.GetByIdAsync(cartItemId), Times.Once);
    }

    private static CartItem CreateCartItem(int userId, int cartItemId)
    {
        var checkIn = DateTime.UtcNow.Date.AddDays(5);
        var checkOut = checkIn.AddDays(2);

        return new CartItem(
            userId: userId,
            roomId: 1,
            checkIn: checkIn,
            checkOut: checkOut,
            adults: 2,
            children: 0)
        {
            CartItemId = cartItemId
        };
    }
}