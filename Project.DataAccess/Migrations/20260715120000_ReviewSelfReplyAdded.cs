using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.DataAccess.Migrations
{
    [DbContext(typeof(SmartMenuDbContext))]
    [Migration("20260715120000_ReviewSelfReplyAdded")]
    public class ReviewSelfReplyAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentReviewId",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ParentReviewId",
                table: "Reviews",
                column: "ParentReviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Reviews_ParentReviewId",
                table: "Reviews",
                column: "ParentReviewId",
                principalTable: "Reviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Reviews_ParentReviewId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ParentReviewId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ParentReviewId",
                table: "Reviews");
        }
    }
}
