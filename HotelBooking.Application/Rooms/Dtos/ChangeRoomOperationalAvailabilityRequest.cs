using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Application.Rooms.Dtos;

public class ChangeRoomOperationalAvailabilityRequest
{
    [Required]
    public bool? IsOperationallyAvailable { get; set; }
}
