using HotelBooking.Application.Cart.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Cart.Create;

public class AddCartItemService : IAddCartItemService
{
    private readonly ICartRepository _cartRepository;
    private readonly IAvailableRoomRepository _availableRoomRepository;

    public AddCartItemService(ICartRepository cartRepository, IAvailableRoomRepository availableRoomRepository)
    {
        _cartRepository = cartRepository;
        _availableRoomRepository = availableRoomRepository;
    }

    public async Task AddCartItemAsync(int userId, AddCartItemRequestDto request)
    {
        if (request.CheckIn.Date < DateTime.UtcNow.Date)
        {
            throw new BadRequestException("Check-in date cannot be in the past.");
        }

        if (request.CheckOut.Date <= request.CheckIn.Date)
        {
            throw new BadRequestException("Check-out date must be after check-in date.");
        }

        if (request.Adults <= 0)
        {
            throw new BadRequestException("At least one adult is required.");
        }

        if (request.Children < 0)
        {
            throw new BadRequestException("Children cannot be negative.");
        }

        var availableRoom = await _availableRoomRepository.GetAvailableRoomAsync(request.RoomId, request.CheckIn, request.CheckOut, request.Adults, request.Children);
        if (availableRoom == null)
        {
            throw new ConflictException("The room is not available for the selected dates and guest requirements.");
        }

        var cartItem = new CartItem(userId, request.RoomId, request.CheckIn, request.CheckOut, request.Adults, request.Children);
        await _cartRepository.AddAsync(cartItem);
        await _cartRepository.SaveChangesAsync();
    }
}