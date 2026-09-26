using System.Collections.Concurrent;
using HotelBooking.Application.Emails;

namespace HotelBooking.IntegrationTests.Infrastructure;

public sealed class TestEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _messages = new();

    public IReadOnlyCollection<SentEmail> Messages => _messages.ToArray();
    public bool ShouldThrow { get; set; }

    public Task SendAsync(string recipientEmail, string subject, string body)
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException("Simulated email failure.");
        }

        _messages.Enqueue(new SentEmail(recipientEmail, subject, body));
        return Task.CompletedTask;
    }

    public void Reset()
    {
        ShouldThrow = false;
        while (_messages.TryDequeue(out _))
        {
        }
    }

    public sealed record SentEmail(string RecipientEmail, string Subject, string Body);
}
