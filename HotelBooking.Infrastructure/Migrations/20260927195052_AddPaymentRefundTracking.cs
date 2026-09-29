using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentRefundTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderRefundId",
                table: "Payments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundFailureCode",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefundStatus",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderRefundId",
                table: "Payments",
                column: "ProviderRefundId",
                unique: true,
                filter: "[ProviderRefundId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ProviderRefundId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderRefundId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RefundFailureCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RefundStatus",
                table: "Payments");
        }
    }
}
