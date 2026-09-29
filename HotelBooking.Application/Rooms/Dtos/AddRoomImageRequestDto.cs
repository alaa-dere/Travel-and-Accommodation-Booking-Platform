using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Application.Rooms.Dtos;

public class AddRoomImageRequestDto
{
    [Required]
    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int DisplayOrder { get; set; }
}
