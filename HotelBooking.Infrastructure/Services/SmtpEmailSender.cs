using System.Net;
using System.Net.Mail;
using HotelBooking.Application.Common.Settings;
using HotelBooking.Application.Emails;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string recipientEmail, string subject, string body)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            message.To.Add(recipientEmail);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            await client.SendMailAsync(message);

            _logger.LogInformation("Booking confirmation email sent successfully.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send booking confirmation email.");
            throw;
        }
    }
}