using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Application.Rooms.Dtos;

public class ChangeRoomStatusRequest
{
    [Required]
    public bool? IsActive { get; set; }
}
