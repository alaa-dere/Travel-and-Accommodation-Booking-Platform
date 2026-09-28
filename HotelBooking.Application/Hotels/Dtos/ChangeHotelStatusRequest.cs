using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Application.Hotels.Dtos;

public class ChangeHotelStatusRequest
{
    [Required]
    public bool? IsActive { get; set; }
}
