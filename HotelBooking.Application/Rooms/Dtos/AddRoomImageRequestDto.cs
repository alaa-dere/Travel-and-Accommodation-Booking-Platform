namespace HotelBooking.Application.Rooms.Dtos;

public class AddRoomImageRequestDto
{
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}