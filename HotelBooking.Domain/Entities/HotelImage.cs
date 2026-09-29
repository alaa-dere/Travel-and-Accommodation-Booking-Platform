namespace HotelBooking.Domain.Entities;

public class HotelImage
{
    public int HotelImageId { get; set; }
    public int HotelId { get; set; }
    public string ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public Hotel? Hotel { get; set; }

    public HotelImage(string imageUrl, int displayOrder,  int hotelId)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("ImageUrl cannot be null or empty", nameof(imageUrl));
        }

        if (displayOrder <= 0)
        {
            throw new ArgumentException("DisplayOrder must be greater than zero", nameof(displayOrder));
        }

        if (imageUrl.Length > 500)
        {
            throw new ArgumentException("ImageUrl cannot exceed 500 characters", nameof(imageUrl));
        }

        if (hotelId <= 0)
        {
            throw new ArgumentException("HotelId must be greater than zero", nameof(hotelId));
        }
        
        ImageUrl = imageUrl.Trim();
        DisplayOrder = displayOrder;
        HotelId = hotelId;
    }
}
