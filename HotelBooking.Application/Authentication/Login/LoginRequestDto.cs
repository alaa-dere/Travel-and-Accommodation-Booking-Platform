using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Application.Authentication.Login;

public class LoginRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}