using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheRewind.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMovieFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ReleaseYear", table: "Movies");

            migrationBuilder
                .AddColumn<string>(name: "Email", table: "Users", type: "longtext", nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AlterColumn<string>(
                    name: "Title",
                    table: "Movies",
                    type: "longtext",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "varchar(120)",
                    oldMaxLength: 120
                )
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Description",
                keyValue: null,
                column: "Description",
                value: ""
            );

            migrationBuilder
                .AlterColumn<string>(
                    name: "Description",
                    table: "Movies",
                    type: "longtext",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "varchar(500)",
                    oldMaxLength: 500,
                    oldNullable: true
                )
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AddColumn<string>(
                    name: "Genre",
                    table: "Movies",
                    type: "longtext",
                    nullable: false
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleaseDate",
                table: "Movies",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Email", table: "Users");

            migrationBuilder.DropColumn(name: "Genre", table: "Movies");

            migrationBuilder.DropColumn(name: "ReleaseDate", table: "Movies");

            migrationBuilder
                .AlterColumn<string>(
                    name: "Title",
                    table: "Movies",
                    type: "varchar(120)",
                    maxLength: 120,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "longtext"
                )
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AlterColumn<string>(
                    name: "Description",
                    table: "Movies",
                    type: "varchar(500)",
                    maxLength: 500,
                    nullable: true,
                    oldClrType: typeof(string),
                    oldType: "longtext"
                )
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ReleaseYear",
                table: "Movies",
                type: "int",
                nullable: true
            );
        }
    }
}
