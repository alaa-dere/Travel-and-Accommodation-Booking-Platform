using HotelBooking.Application.AvailableRooms.Dtos;
using HotelBooking.Application.Exceptions;

namespace HotelBooking.Application.AvailableRooms;

internal static class AvailableRoomsRequestValidator
{
    public static void Validate(AvailableRoomsRequestDto request, DateTime utcNow)
    {
        if (request.CheckIn == default || request.CheckOut == default)
        {
            throw new BadRequestException("Check-in and check-out dates are required.");
        }

        if (request.CheckOut <= request.CheckIn)
        {
            throw new BadRequestException("Check-out date must be after check-in date.");
        }

        if (request.CheckIn.Date < utcNow.Date)
        {
            throw new BadRequestException("Check-in date cannot be in the past.");
        }

        if (request.Adults <= 0)
        {
            throw new BadRequestException("Adults must be greater than zero.");
        }

        if (request.Children < 0)
        {
            throw new BadRequestException("Children cannot be negative.");
        }
    }
}
