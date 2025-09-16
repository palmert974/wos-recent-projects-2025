using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheVinylCountdown.Migrations
{
    /// <inheritdoc />
    public partial class AddGenreAndReleaseYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Genre",
                table: "Albums",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ReleaseYear",
                table: "Albums",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Genre",
                table: "Albums");

            migrationBuilder.DropColumn(
                name: "ReleaseYear",
                table: "Albums");
        }
    }
}
