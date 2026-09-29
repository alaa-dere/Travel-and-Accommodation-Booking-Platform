using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionLookupIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Promotions_HotelId",
                table: "Promotions");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_HotelId_IsActive_StartDate_EndDate",
                table: "Promotions",
                columns: new[] { "HotelId", "IsActive", "StartDate", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Promotions_HotelId_IsActive_StartDate_EndDate",
                table: "Promotions");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_HotelId",
                table: "Promotions",
                column: "HotelId");
        }
    }
}
