using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace libraryAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesForHotQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "friend_requests",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "books",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "author",
                table: "books",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_friend_requests_to_user_id_status",
                table: "friend_requests",
                columns: new[] { "to_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_activity_user_id_date",
                table: "daily_activity",
                columns: new[] { "user_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_books_author",
                table: "books",
                column: "author");

            migrationBuilder.CreateIndex(
                name: "IX_books_created_at",
                table: "books",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_books_title",
                table: "books",
                column: "title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_friend_requests_to_user_id_status",
                table: "friend_requests");

            migrationBuilder.DropIndex(
                name: "IX_daily_activity_user_id_date",
                table: "daily_activity");

            migrationBuilder.DropIndex(
                name: "IX_books_author",
                table: "books");

            migrationBuilder.DropIndex(
                name: "IX_books_created_at",
                table: "books");

            migrationBuilder.DropIndex(
                name: "IX_books_title",
                table: "books");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "friend_requests",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "books",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "author",
                table: "books",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
