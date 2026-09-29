namespace HotelBooking.Domain.ValueObjects;

public sealed record BookingPrice
{
    public decimal PricePerNight { get; }
    public decimal OriginalTotal { get; }
    public int DiscountPercentage { get; }
    public decimal DiscountAmount { get; }
    public decimal Total { get; }

    public BookingPrice(decimal pricePerNight, decimal originalTotal, int discountPercentage, decimal discountAmount, decimal total)
    {
        if (pricePerNight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pricePerNight), "Price per night must be greater than zero.");
        }     
        if (originalTotal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(originalTotal), "Original total price must be greater than zero.");
        }       
        if (discountPercentage is < 0 or >= 100)
        {
            throw new ArgumentOutOfRangeException(nameof(discountPercentage), "Discount percentage must be between 0 and 100.");
        }      
        if (discountAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discountAmount), "Discount amount cannot be negative.");
        }       
        if (total <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(total), "Total price must be greater than zero.");
        }       
        if (total != originalTotal - discountAmount)
        {
            throw new ArgumentException("Total price must equal the original total minus the discount amount.", nameof(total));
        }
        PricePerNight = pricePerNight;
        OriginalTotal = originalTotal;
        DiscountPercentage = discountPercentage;
        DiscountAmount = discountAmount;
        Total = total;
    }
}
