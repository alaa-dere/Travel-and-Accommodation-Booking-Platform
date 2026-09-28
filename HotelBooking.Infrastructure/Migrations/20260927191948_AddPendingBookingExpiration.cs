using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingBookingExpiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PendingExpiresAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [Bookings] SET [PendingExpiresAt] = DATEADD(minute, 15, [CreatedAt])");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PendingExpiresAt",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingStatus_PendingExpiresAt",
                table: "Bookings",
                columns: new[] { "BookingStatus", "PendingExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookingStatus_PendingExpiresAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PendingExpiresAt",
                table: "Bookings");
        }
    }
}
