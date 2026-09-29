namespace HotelBooking.Application.Common.Settings;

public sealed class BookingSettings
{
    public const string SectionName = "Booking";
    public int PendingExpirationMinutes { get; set; } = 15;
    public int ExpirationBatchSize { get; set; } = 100;
    public int ExpirationCheckIntervalSeconds { get; set; } = 30;
    public bool EnableExpirationWorker { get; set; } = true;
}
