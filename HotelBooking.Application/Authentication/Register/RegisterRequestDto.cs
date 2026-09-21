using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Application.Authentication.Register;

public class RegisterRequestDto
{
    [Required]
    [StringLength(100,MinimumLength = 2)]
    public string FirstName { get; set; }
    [Required]
    [StringLength(100,MinimumLength = 2)]
    public string LastName { get; set; }
    [Required]
    [StringLength(100,MinimumLength = 2)]
    public string Username { get; set; }
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; }
    [Required]
    [MinLength(8)]
    public string Password { get; set; }
}