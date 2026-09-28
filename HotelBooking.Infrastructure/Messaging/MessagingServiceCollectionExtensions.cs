using HotelBooking.Application.Common.Settings;
using HotelBooking.Application.Emails;
using HotelBooking.Application.Messaging;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotelBooking.Infrastructure.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddHotelBookingMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddScoped<BookingConfirmationEmailService>();
        services.AddScoped<IBookingConfirmationEmailService, QueuedBookingConfirmationEmailService>();
        services.AddScoped<IBackgroundTaskPublisher, MassTransitBackgroundTaskPublisher>();

        if (environment.IsEnvironment("Testing"))
        {
            services.AddScoped<IBookingConfirmationEmailService, BookingConfirmationEmailService>();
        }

        services.AddMassTransit(registration =>
        {
            registration.AddConsumer<SendBookingConfirmationConsumer>();
            registration.AddConsumer<RetryPaymentRefundConsumer>();

            if (environment.IsEnvironment("Testing"))
            {
                registration.UsingInMemory((context, bus) =>
                {
                    bus.UseMessageRetry(retry => retry.Intervals(
                        TimeSpan.FromMilliseconds(10),
                        TimeSpan.FromMilliseconds(25),
                        TimeSpan.FromMilliseconds(50)));
                    bus.ConfigureEndpoints(context);
                });
                return;
            }

            var settings = new RabbitMqSettings
            {
                Host = configuration["RabbitMq:Host"] ?? "localhost",
                Username = configuration["RabbitMq:Username"] ?? "guest",
                Password = configuration["RabbitMq:Password"] ?? "guest"
            };
            registration.UsingRabbitMq((context, bus) =>
            {
                bus.Host(new Uri($"rabbitmq://{settings.Host}/"), host =>
                {
                    host.Username(settings.Username);
                    host.Password(settings.Password);
                });
                bus.UseMessageRetry(retry => retry.Intervals(
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromMinutes(2)));
                bus.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
