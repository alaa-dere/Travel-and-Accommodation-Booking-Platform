namespace HotelBooking.Application.Cart.Dtos;

public class AddCartItemRequestDto
{
    public int RoomId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
}