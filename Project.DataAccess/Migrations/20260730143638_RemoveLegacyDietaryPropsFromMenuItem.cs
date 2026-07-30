using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyDietaryPropsFromMenuItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContainsGluten",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ContainsNuts",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IsVegan",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IsVegetarian",
                table: "MenuItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ContainsGluten",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ContainsNuts",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVegan",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVegetarian",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
