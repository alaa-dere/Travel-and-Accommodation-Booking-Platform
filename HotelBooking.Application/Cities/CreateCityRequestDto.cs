using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Application.Cities;

public class CreateCityRequestDto
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }  = string.Empty;
    [Required]
    [MaxLength(50)]
    public string Country { get; set; }  = string.Empty;
    [Required]
    public string PostOffice  { get; set; }  = string.Empty;
}