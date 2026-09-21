using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace libraryAPI.Migrations;

public partial class AddRecommendationFeedback : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "recommendation_feedback",
            columns: table => new
            {
                id = table.Column<string>(type: "varchar(255)", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                user_id = table.Column<string>(type: "varchar(255)", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                book_id = table.Column<string>(type: "varchar(255)", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                is_positive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recommendation_feedback", x => x.id);
                table.ForeignKey(
                    name: "FK_recommendation_feedback_books_book_id",
                    column: x => x.book_id,
                    principalTable: "books",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_recommendation_feedback_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_recommendation_feedback_book_id",
            table: "recommendation_feedback",
            column: "book_id");

        migrationBuilder.CreateIndex(
            name: "IX_recommendation_feedback_user_id_book_id",
            table: "recommendation_feedback",
            columns: new[] { "user_id", "book_id" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "recommendation_feedback");
    }
}
