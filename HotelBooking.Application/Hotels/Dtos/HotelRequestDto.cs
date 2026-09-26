using System.ComponentModel.DataAnnotations;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Hotels.Dtos;

public class HotelRequestDto
{
    [Range(1, int.MaxValue)]
    public int CityId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string OwnerName { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Range(typeof(double), "-90", "90")]
    public double Latitude { get; set; }

    [Range(typeof(double), "-180", "180")]
    public double Longitude { get; set; }

    [EnumDataType(typeof(HotelType))]
    public HotelType HotelType { get; set; }
    public string? Description { get; set; } 
    public string? History { get; set; } 
}
