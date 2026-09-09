using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace libraryAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEquippedTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "equipped_title",
                table: "users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "equipped_title",
                table: "users");
        }
    }
}
