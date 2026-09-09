using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace libraryAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteAndPagesToBooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "pages",
                table: "books",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "quote",
                table: "books",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pages",
                table: "books");

            migrationBuilder.DropColumn(
                name: "quote",
                table: "books");
        }
    }
}
