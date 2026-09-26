using System.ComponentModel.DataAnnotations;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Rooms.Dtos;

public class RoomRequestDto
{
    [Range(1, int.MaxValue)]
    public int HotelId  { get; set; }

    [Required]
    [StringLength(50)]
    public string RoomNumber { get; set; } = string.Empty;

    [EnumDataType(typeof(RoomType))]
    public RoomType RoomType { get; set; }

    [Range(1, int.MaxValue)]
    public int AdultsCapacity  { get; set; }

    [Range(0, int.MaxValue)]
    public int ChildCapacity { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal PricePerNight { get; set; } 
    public string? Description { get; set; }
}
