using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheRewind.Migrations
{
    /// <inheritdoc />
    public partial class InitalCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase().Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Users",
                    columns: table => new
                    {
                        Id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        Username = table
                            .Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        PasswordHash = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Users", x => x.Id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Movies",
                    columns: table => new
                    {
                        Id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        Title = table
                            .Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Description = table
                            .Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        ReleaseYear = table.Column<int>(type: "int", nullable: true),
                        UserId = table.Column<int>(type: "int", nullable: false),
                        CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Movies", x => x.Id);
                        table.ForeignKey(
                            name: "FK_Movies_Users_UserId",
                            column: x => x.UserId,
                            principalTable: "Users",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Ratings",
                    columns: table => new
                    {
                        Id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        Value = table.Column<int>(type: "int", nullable: false),
                        UserId = table.Column<int>(type: "int", nullable: false),
                        MovieId = table.Column<int>(type: "int", nullable: false),
                        CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Ratings", x => x.Id);
                        table.ForeignKey(
                            name: "FK_Ratings_Movies_MovieId",
                            column: x => x.MovieId,
                            principalTable: "Movies",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_Ratings_Users_UserId",
                            column: x => x.UserId,
                            principalTable: "Users",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_UserId",
                table: "Movies",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_MovieId",
                table: "Ratings",
                column: "MovieId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_UserId_MovieId",
                table: "Ratings",
                columns: new[] { "UserId", "MovieId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Ratings");

            migrationBuilder.DropTable(name: "Movies");

            migrationBuilder.DropTable(name: "Users");
        }
    }
}
