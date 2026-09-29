using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Application.Bookings.Dtos;

public class ModifyBookingRequestDto
{
    [Range(1, int.MaxValue)]
    public int Adults { get; set; }

    [Range(0, int.MaxValue)]
    public int Children { get; set; }

    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }
}
