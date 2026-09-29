using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueImageAndAttractionOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoomImages_RoomId",
                table: "RoomImages");

            migrationBuilder.DropIndex(
                name: "IX_NearbyAttractions_HotelId",
                table: "NearbyAttractions");

            migrationBuilder.DropIndex(
                name: "IX_HotelImages_HotelId",
                table: "HotelImages");

            migrationBuilder.CreateIndex(
                name: "IX_RoomImages_RoomId_DisplayOrder",
                table: "RoomImages",
                columns: new[] { "RoomId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomImages_RoomId_ImageUrl",
                table: "RoomImages",
                columns: new[] { "RoomId", "ImageUrl" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NearbyAttractions_HotelId_Name",
                table: "NearbyAttractions",
                columns: new[] { "HotelId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotelImages_HotelId_DisplayOrder",
                table: "HotelImages",
                columns: new[] { "HotelId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotelImages_HotelId_ImageUrl",
                table: "HotelImages",
                columns: new[] { "HotelId", "ImageUrl" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoomImages_RoomId_DisplayOrder",
                table: "RoomImages");

            migrationBuilder.DropIndex(
                name: "IX_RoomImages_RoomId_ImageUrl",
                table: "RoomImages");

            migrationBuilder.DropIndex(
                name: "IX_NearbyAttractions_HotelId_Name",
                table: "NearbyAttractions");

            migrationBuilder.DropIndex(
                name: "IX_HotelImages_HotelId_DisplayOrder",
                table: "HotelImages");

            migrationBuilder.DropIndex(
                name: "IX_HotelImages_HotelId_ImageUrl",
                table: "HotelImages");

            migrationBuilder.CreateIndex(
                name: "IX_RoomImages_RoomId",
                table: "RoomImages",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_NearbyAttractions_HotelId",
                table: "NearbyAttractions",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelImages_HotelId",
                table: "HotelImages",
                column: "HotelId");
        }
    }
}
