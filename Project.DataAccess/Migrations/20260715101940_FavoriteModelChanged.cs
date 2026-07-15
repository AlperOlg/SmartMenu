using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FavoriteModelChanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Favorites_AppUserId_RestaurantId",
                table: "Favorites");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_AppUserId",
                table: "Favorites",
                column: "AppUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Favorites_AppUserId",
                table: "Favorites");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_AppUserId_RestaurantId",
                table: "Favorites",
                columns: new[] { "AppUserId", "RestaurantId" },
                unique: true);
        }
    }
}
