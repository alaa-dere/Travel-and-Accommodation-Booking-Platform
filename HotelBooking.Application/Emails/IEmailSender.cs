namespace HotelBooking.Application.Emails;

public interface IEmailSender
{
    Task SendAsync(string recipientEmail, string subject, string body);
}