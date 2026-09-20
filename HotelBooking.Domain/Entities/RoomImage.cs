namespace HotelBooking.Domain.Entities;

public class RoomImage
{
    public int RoomImageId { get; set; }
    public string ImageUrl { get; set; }
    public int DisplayOrder { get; set; }

    public RoomImage(string imageUrl, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("ImageUrl cannot be null or empty", nameof(imageUrl));
        }

        if (displayOrder <= 0)
        {
            throw new ArgumentException("DisplayOrder must be greater than zero", nameof(displayOrder));
        }
        
        ImageUrl  = imageUrl;
        DisplayOrder = displayOrder;
    }
}