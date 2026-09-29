using HotelBooking.Domain.ValueObjects;
using HotelBooking.Domain.Enums;

namespace HotelBooking.Domain.Entities;

public class Booking
{
    private Booking()
    {
    }

    public int BookingId { get; internal set; }
    public int UserId { get; private set; }
    public int RoomId { get; private set; }
    public int InvoiceId { get; internal set; }
    public DateTime CheckIn { get; private set; }
    public DateTime CheckOut { get; private set; }
    public int Adults { get; private set; }
    public int Children { get; private set; }
    public decimal PricePerNight  { get; private set; }
    public decimal OriginalTotalPrice { get; private set; }
    public int DiscountPercentage { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalPrice { get; private set; }
    public BookingStatus BookingStatus  { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime PendingExpiresAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? SpecialRequests { get; private set; }
    public decimal? RefundedAmount { get; private set; }
    public string? ProviderRefundId { get; private set; }
    public RefundStatus? RefundStatus { get; private set; }
    public string? RefundFailureCode { get; private set; }
    public User? User { get; internal set; }
    public Room? Room { get; internal set; }
    public Review? Review { get; internal set; }
    public Invoice? Invoice { get; internal set; }

    public Booking(int userId, BookingStay stay, BookingPrice price, PendingBookingWindow pendingWindow, string? specialRequests)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "User ID must be greater than zero.");
        }    
        ValidateSpecialRequests(specialRequests);

        UserId = userId;
        ApplyStay(stay);
        ApplyPrice(price);
        BookingStatus = BookingStatus.Pending;
        CreatedAt = pendingWindow.CreatedAt;
        PendingExpiresAt = pendingWindow.ExpiresAt;
        SpecialRequests = specialRequests;
    }

    
    public void Cancel(DateTime utcNow)
    {
        ValidateUtc(utcNow, nameof(utcNow));
        if (BookingStatus is not (BookingStatus.Pending or BookingStatus.Confirmed))
        {
            throw new InvalidOperationException("Only a pending or confirmed booking can be cancelled.");
        }
        BookingStatus = BookingStatus.Cancelled;
        UpdatedAt = utcNow;
    }

    public void CancelByCustomer(DateTime utcNow)
    {
        ValidateUtc(utcNow, nameof(utcNow));
        if (utcNow >= CheckIn)
        {
            throw new InvalidOperationException("A booking cannot be cancelled after the stay has started.");
        }
        Cancel(utcNow);
    }

    public void Confirm(DateTime utcNow)
    {
        ValidateUtc(utcNow, nameof(utcNow));
        if (BookingStatus != BookingStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending booking can be confirmed.");
        }
        BookingStatus = BookingStatus.Confirmed;
        UpdatedAt = utcNow;
    }

    public void Expire(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Expiration check time must be in UTC.", nameof(utcNow));
        }

        if (BookingStatus != BookingStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending booking can expire.");
        }

        if (PendingExpiresAt > utcNow)
        {
            throw new InvalidOperationException("A pending booking cannot expire before its payment deadline.");
        }

        BookingStatus = BookingStatus.Cancelled;
        UpdatedAt = utcNow;
    }

    public void Complete(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Completion time must be in UTC.", nameof(utcNow));
        }

        if (BookingStatus != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException("Only a confirmed booking can be completed.");
        }

        if (utcNow < CheckOut)
        {
            throw new InvalidOperationException("A booking cannot be completed before check-out.");
        }

        BookingStatus = BookingStatus.Completed;
        UpdatedAt = utcNow;
    }

    public void Modify(BookingStay stay, BookingPrice price, string? specialRequests, DateTime utcNow)
    {
        ValidateUtc(utcNow, nameof(utcNow));
        if (BookingStatus is not (BookingStatus.Pending or BookingStatus.Confirmed))
        {
            throw new InvalidOperationException("Only a pending or confirmed booking can be modified.");
        }     
        if (utcNow >= CheckIn)
        {
            throw new InvalidOperationException("A booking cannot be modified after the stay has started.");
        }
        ValidateSpecialRequests(specialRequests);
        ApplyStay(stay);
        ApplyPrice(price);
        SpecialRequests = specialRequests;
        UpdatedAt = utcNow;
    }

    public void UpdateGuestDetails(int adults, int children, string? specialRequests, DateTime utcNow)
    {
        ValidateUtc(utcNow, nameof(utcNow));
        if (BookingStatus is not (BookingStatus.Pending or BookingStatus.Confirmed))
        {
            throw new InvalidOperationException("Only a pending or confirmed booking can be modified.");
        }
        if (utcNow >= CheckIn)
        {
            throw new InvalidOperationException("A booking cannot be modified after the stay has started.");
        }
        if (adults < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(adults), "At least one adult is required.");
        }
        if (children < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(children), "Children count cannot be negative.");
        }

        ValidateSpecialRequests(specialRequests);
        Adults = adults;
        Children = children;
        SpecialRequests = string.IsNullOrWhiteSpace(specialRequests) ? null : specialRequests.Trim();
        UpdatedAt = utcNow;
    }

    public void RecordCustomerRefund(decimal amount, string providerRefundId, RefundStatus status, string? failureCode, DateTime utcNow)
    {
        ValidateUtc(utcNow, nameof(utcNow));
        if (amount <= 0 || amount > TotalPrice)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Refund amount must be a valid booking amount.");
        }
        if (string.IsNullOrWhiteSpace(providerRefundId))
        {
            throw new ArgumentException("Provider refund ID is required.", nameof(providerRefundId));
        }
        if (ProviderRefundId is not null && ProviderRefundId != providerRefundId)
        {
            throw new InvalidOperationException("The booking is already linked to another refund.");
        }

        RefundedAmount = amount;
        ProviderRefundId = providerRefundId;
        RefundStatus = status;
        RefundFailureCode = failureCode;
        UpdatedAt = utcNow;
    }

    public void AssignToInvoice(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (invoice.UserId != UserId)
        {
            throw new InvalidOperationException("A booking and its invoice must belong to the same user.");
        }
        if (Invoice is not null && !ReferenceEquals(Invoice, invoice))
        {
            throw new InvalidOperationException("The booking is already assigned to another invoice.");
        }
        Invoice = invoice;
        if (!invoice.Bookings.Contains(this))
        {
            invoice.Bookings.Add(this);
        }
        
    }

    private static void ValidateSpecialRequests(string? specialRequests)
    {
        if (specialRequests?.Length > 1000)
        {
            throw new ArgumentException("Special requests cannot exceed 1000 characters.", nameof(specialRequests));
        }
    }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The supplied time must be in UTC.", parameterName);
        }
    }

    private void ApplyStay(BookingStay stay)
    {
        RoomId = stay.RoomId;
        CheckIn = stay.CheckIn;
        CheckOut = stay.CheckOut;
        Adults = stay.Adults;
        Children = stay.Children;
    }

    private void ApplyPrice(BookingPrice price)
    {
        PricePerNight = price.PricePerNight;
        OriginalTotalPrice = price.OriginalTotal;
        DiscountPercentage = price.DiscountPercentage;
        DiscountAmount = price.DiscountAmount;
        TotalPrice = price.Total;
    }
}
