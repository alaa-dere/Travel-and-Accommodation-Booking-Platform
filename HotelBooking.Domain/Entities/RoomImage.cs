namespace HotelBooking.Domain.Entities;

public class RoomImage
{
    public int RoomImageId { get; set; }
    public int RoomId { get; set; }
    public string ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public Room? Room { get; set; }

    public RoomImage(string imageUrl, int displayOrder,   int roomId)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("ImageUrl cannot be null or empty", nameof(imageUrl));
        }

        if (displayOrder <= 0)
        {
            throw new ArgumentException("DisplayOrder must be greater than zero", nameof(displayOrder));
        }

        if (roomId <= 0)
        {
            throw new ArgumentException("RoomId must be greater than zero", nameof(roomId));
        }
        ImageUrl  = imageUrl;
        DisplayOrder = displayOrder;
        RoomId = roomId;
    }
}