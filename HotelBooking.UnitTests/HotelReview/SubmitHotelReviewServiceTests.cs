using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelReviews;
using HotelBooking.Application.HotelReviews.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.HotelReviews;

public class SubmitHotelReviewServiceTests
{
    private readonly Mock<IHotelReviewRepository> _repositoryMock;
    private readonly SubmitHotelReviewService _service;

    public SubmitHotelReviewServiceTests()
    {
        _repositoryMock = new Mock<IHotelReviewRepository>();

        _service = new SubmitHotelReviewService(
            _repositoryMock.Object);
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenRatingIsLessThanOne_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Rating = 0;

        // Act
        var action = async () =>
            await _service.SubmitReviewAsync(1, 1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _repositoryMock.Verify(
            repository => repository.GetBookingForReviewAsync(
                It.IsAny<int>()),
            Times.Never);

        VerifyReviewWasNotSaved();
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenRatingIsGreaterThanFive_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Rating = 6;

        // Act
        var action = async () =>
            await _service.SubmitReviewAsync(1, 1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _repositoryMock.Verify(
            repository => repository.GetBookingForReviewAsync(
                It.IsAny<int>()),
            Times.Never);

        VerifyReviewWasNotSaved();
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenCommentIsEmpty_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Comment = string.Empty;

        // Act
        var action = async () =>
            await _service.SubmitReviewAsync(1, 1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _repositoryMock.Verify(
            repository => repository.GetBookingForReviewAsync(
                It.IsAny<int>()),
            Times.Never);

        VerifyReviewWasNotSaved();
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenCommentIsWhitespace_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Comment = "   ";

        // Act
        var action = async () =>
            await _service.SubmitReviewAsync(1, 1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _repositoryMock.Verify(
            repository => repository.GetBookingForReviewAsync(
                It.IsAny<int>()),
            Times.Never);

        VerifyReviewWasNotSaved();
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenBookingDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 1;

        var request = CreateValidRequest();

        _repositoryMock
            .Setup(repository =>
                repository.GetBookingForReviewAsync(request.BookingId))
            .ReturnsAsync((Booking?)null);

        // Act
        var action = async () =>
            await _service.SubmitReviewAsync(
                hotelId,
                userId,
                request);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        VerifyReviewWasNotSaved();
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenBookingBelongsToAnotherUser_ShouldThrowBadRequestException()
    {
        // Arrange
        const int hotelId = 10;
        const int currentUserId = 1;

        var request = CreateValidRequest();

        var booking = CreateValidCompletedBooking(
            userId: 2,
            hotelId: hotelId,
            bookingId: request.BookingId);

        SetupBooking(request.BookingId, booking);

        // Act
        var action = async () =>
            await _service.SubmitReviewAsync(
                hotelId,
                currentUserId,
                request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyReviewWasNotSaved();
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenBookingRoomIsNull_ShouldThrowBadRequestException()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 1;

        var request = CreateValidRequest();

        var booking = CreateValidCompletedBooking(
            userId,
            hotelId,
            request.BookingId);

        booking.Room = null;

        SetupBooking(request.BookingId, booking);

        // Act
        var action = async () =>
            await _service.SubmitReviewAsync(
                hotelId,
                userId,
                request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyReviewWasNotSaved();
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenBookingBelongsToDifferentHotel_ShouldThrowBadRequestException()
    {
        // Arrange
        const int requestedHotelId = 10;
        const int bookingHotelId = 20;
        const int userId = 1;

        var request = CreateValidRequest();

        var booking = CreateValidCompletedBooking(
            userId,
            bookingHotelId,
            request.BookingId);

        SetupBooking(request.BookingId, booking);

        // Act
        var action = async () =>
            await _service.SubmitReviewAsync(
                requestedHotelId,
                userId,
                request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyReviewWasNotSaved();
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenBookingIsNotCompleted_ShouldThrowBadRequestException()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 1;

        var request = CreateValidRequest();

        var booking = CreateValidCompletedBooking(
            userId,
            hotelId,
            request.BookingId);

        booking.BookingStatus = BookingStatus.Confirmed;

        SetupBooking(request.BookingId, booking);

        // Act
        var action = async () =>
            await _service.SubmitReviewAsync(
                hotelId,
                userId,
                request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyReviewWasNotSaved();
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenCheckoutIsInFuture_ShouldThrowBadRequestException()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 1;

        var request = CreateValidRequest();

        var booking = CreateValidCompletedBooking(
            userId,
            hotelId,
            request.BookingId);

        booking.CheckOut = DateTime.UtcNow.AddDays(1);

        SetupBooking(request.BookingId, booking);

        // Act
        var action = async () =>
            await _service.SubmitReviewAsync(
                hotelId,
                userId,
                request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyReviewWasNotSaved();
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenReviewAlreadyExists_ShouldThrowBadRequestException()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 1;

        var request = CreateValidRequest();

        var booking = CreateValidCompletedBooking(
            userId,
            hotelId,
            request.BookingId);

        booking.Review = new Review(
            bookingId: request.BookingId,
            rating: 4,
            comment: "Existing review");

        SetupBooking(request.BookingId, booking);

        // Act
        var action = async () =>
            await _service.SubmitReviewAsync(
                hotelId,
                userId,
                request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyReviewWasNotSaved();
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenRequestIsValid_ShouldCreateCorrectReview()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 1;

        var request = CreateValidRequest();

        var booking = CreateValidCompletedBooking(
            userId,
            hotelId,
            request.BookingId);

        SetupBooking(request.BookingId, booking);

        Review? addedReview = null;

        _repositoryMock
            .Setup(repository =>
                repository.AddReviewAsync(It.IsAny<Review>()))
            .Callback<Review>(review =>
                addedReview = review);

        // Act
        await _service.SubmitReviewAsync(
            hotelId,
            userId,
            request);

        // Assert
        Assert.NotNull(addedReview);

        Assert.Equal(
            request.BookingId,
            addedReview!.BookingId);

        Assert.Equal(
            request.Rating,
            addedReview.Rating);

        Assert.Equal(
            request.Comment,
            addedReview.Comment);
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenRequestIsValid_ShouldAddReviewOnce()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 1;

        var request = CreateValidRequest();

        var booking = CreateValidCompletedBooking(
            userId,
            hotelId,
            request.BookingId);

        SetupBooking(request.BookingId, booking);

        // Act
        await _service.SubmitReviewAsync(
            hotelId,
            userId,
            request);

        // Assert
        _repositoryMock.Verify(
            repository => repository.AddReviewAsync(
                It.Is<Review>(review =>
                    review.BookingId == request.BookingId &&
                    review.Rating == request.Rating &&
                    review.Comment == request.Comment)),
            Times.Once);
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenRequestIsValid_ShouldSaveChangesOnce()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 1;

        var request = CreateValidRequest();

        var booking = CreateValidCompletedBooking(
            userId,
            hotelId,
            request.BookingId);

        SetupBooking(request.BookingId, booking);

        // Act
        await _service.SubmitReviewAsync(
            hotelId,
            userId,
            request);

        // Assert
        _repositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task SubmitReviewAsync_ShouldRequestCorrectBooking()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 1;

        var request = CreateValidRequest();

        var booking = CreateValidCompletedBooking(
            userId,
            hotelId,
            request.BookingId);

        SetupBooking(request.BookingId, booking);

        // Act
        await _service.SubmitReviewAsync(
            hotelId,
            userId,
            request);

        // Assert
        _repositoryMock.Verify(
            repository =>
                repository.GetBookingForReviewAsync(
                    request.BookingId),
            Times.Once);
    }

    private void SetupBooking(
        int bookingId,
        Booking booking)
    {
        _repositoryMock
            .Setup(repository => repository.GetBookingForReviewAsync(bookingId))
            .ReturnsAsync(booking);
    }

    private void VerifyReviewWasNotSaved()
    {
        _repositoryMock.Verify(repository => repository.AddReviewAsync(It.IsAny<Review>()), Times.Never);

        _repositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    private static SubmitReviewRequestDto CreateValidRequest()
    {
        return new SubmitReviewRequestDto
        {
            BookingId = 100,
            Rating = 5,
            Comment = "Excellent hotel."
        };
    }

    private static Booking CreateValidCompletedBooking(
        int userId,
        int hotelId,
        int bookingId)
    {
        var checkIn = DateTime.UtcNow.AddDays(-5);
        var checkOut = DateTime.UtcNow.AddDays(-2);

        var room = new Room(
            roomNumber: "101",
            roomType: (RoomType)1,
            pricePerNight: 100m,
            adultsCapacity: 2,
            childCapacity: 2,
            hotelId: hotelId,
            description: null)
        {
            RoomId = 20
        };

        var booking = new Booking(
            userId: userId,
            roomId: room.RoomId,
            checkIn: checkIn,
            checkOut: checkOut,
            adults: 2,
            children: 0,
            pricePerNight: 100m,
            originalTotalPrice: 300m,
            discountPercentage: 0,
            discountAmount: 0m,
            totalPrice: 300m,
            specialRequests: null)
        {
            BookingId = bookingId,
            Room = room,
            BookingStatus = BookingStatus.Completed
        };

        return booking;
    }
}