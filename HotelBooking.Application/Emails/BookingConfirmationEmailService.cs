using System.Text;
using HotelBooking.Application.Emails.Dtos;

namespace HotelBooking.Application.Emails;

public class BookingConfirmationEmailService : IBookingConfirmationEmailService
{
    private readonly IEmailSender _emailSender;

    public BookingConfirmationEmailService(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task SendAsync(BookingConfirmationEmailDto confirmation)
    {
        var subject = $"Booking Confirmation - Invoice #{confirmation.InvoiceId}";
        var body = BuildEmailBody(confirmation);

        await _emailSender.SendAsync(confirmation.CustomerEmail, subject, body);
    }

    private static string BuildEmailBody(BookingConfirmationEmailDto confirmation)
    {
        var body = new StringBuilder();

        body.AppendLine("Your booking has been confirmed.");
        body.AppendLine();
        body.AppendLine("BOOKING CONFIRMATION");
        body.AppendLine("--------------------");
        body.AppendLine($"Hotel: {confirmation.HotelName}");
        body.AppendLine();

        foreach (var room in confirmation.Rooms)
        {
            body.AppendLine($"Booking #{room.BookingId}");
            body.AppendLine($"Room: {room.RoomNumber}");
            body.AppendLine($"Check-in: {room.CheckIn:yyyy-MM-dd}");
            body.AppendLine($"Check-out: {room.CheckOut:yyyy-MM-dd}");
            body.AppendLine($"Booking Total: {room.TotalPrice:0.00}");
            body.AppendLine();
        }

        body.AppendLine("INVOICE INFORMATION");
        body.AppendLine("-------------------");
        body.AppendLine($"Invoice #{confirmation.InvoiceId}");
        body.AppendLine($"Invoice Total: {confirmation.InvoiceTotal:0.00}");
        body.AppendLine($"Payment Status: {confirmation.PaymentStatus}");
        body.AppendLine();
        body.AppendLine("You can download the full invoice PDF from your account.");

        return body.ToString();
    }
}