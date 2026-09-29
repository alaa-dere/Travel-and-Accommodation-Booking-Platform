using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Invoices.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HotelBooking.Infrastructure.Services;

public class InvoicePdfGenerator : IInvoicePdfGenerator
{
    public byte[] Generate(InvoicePdfDto invoice)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);

                page.Header()
                    .Text($"Invoice #{invoice.InvoiceId}")
                    .FontSize(22)
                    .Bold();

                page.Content()
                    .PaddingVertical(20)
                    .Column(column =>
                    {
                        column.Spacing(10);

                        column.Item()
                            .Text($"Hotel: {invoice.HotelName}");

                        column.Item()
                            .Text($"Address: {invoice.HotelAddress}");

                        column.Item()
                            .Text($"Invoice Date: {invoice.CreatedAt:yyyy-MM-dd}");

                        column.Item()
                            .Text($"Payment Status: {invoice.PaymentStatus}");

                        foreach (var booking in invoice.Bookings)
                        {
                            column.Item()
                                .PaddingTop(10)
                                .Text($"Booking #{booking.BookingId}")
                                .Bold();

                            column.Item()
                                .Text($"Room: {booking.RoomNumber}");

                            column.Item()
                                .Text($"Booking status: {booking.Status}");

                            column.Item()
                                .Text($"Check-in: {booking.CheckIn:yyyy-MM-dd}");

                            column.Item()
                                .Text($"Check-out: {booking.CheckOut:yyyy-MM-dd}");

                            column.Item()
                                .Text($"Price per night: {FormatMoney(booking.PricePerNight)}");

                            column.Item()
                                .Text($"Original price: {FormatMoney(booking.OriginalTotalPrice)}");

                            column.Item()
                                .Text($"Discount: {booking.DiscountPercentage}%");

                            column.Item()
                                .Text($"Discount amount: {FormatMoney(booking.DiscountAmount)}");

                            column.Item()
                                .Text($"Booking total: {FormatMoney(booking.TotalPrice)}");

                            if (!string.IsNullOrWhiteSpace(booking.SpecialRequests))
                            {
                                column.Item()
                                    .Text($"Special requests: {booking.SpecialRequests}");
                            }

                            if (booking.RefundedAmount.HasValue)
                            {
                                column.Item()
                                    .Text($"Refund: {FormatMoney(booking.RefundedAmount.Value)} ({booking.RefundStatus})");
                            }
                        }

                        column.Item()
                            .PaddingTop(20)
                            .Text($"Invoice Total: {FormatMoney(invoice.TotalAmount)}")
                            .FontSize(16)
                            .Bold();
                    });

                page.Footer()
                    .AlignCenter()
                    .Text("Hotel Booking System");
            });
        }).GeneratePdf();
    }

    private static string FormatMoney(decimal amount) => $"{amount:N2} USD";
}
