using HotelBooking.Domain.Entities;

namespace HotelBooking.Application;

public interface IHotelSearchRepository
{
    Task<IEnumerable<Hotel>> GetCandidateHotelsAsync(string destination, DateTime checkIn, DateTime checkOut, int requiredRooms, int adults = 1, int children = 0);
}
    