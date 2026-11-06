using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizBattle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseCaseInsensitivePlayerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_RoomId_DisplayName",
                table: "Players");

            migrationBuilder.CreateIndex(
                name: "IX_Players_RoomId",
                table: "Players",
                column: "RoomId");

            // Index theo biểu thức, EF không khai báo được bằng fluent API (xem PlayerConfiguration). Không dùng citext để khỏi phụ thuộc extension.
            migrationBuilder.Sql(@"CREATE UNIQUE INDEX ""IX_Players_RoomId_lower_DisplayName"" ON ""Players"" (""RoomId"", lower(""DisplayName""));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX ""IX_Players_RoomId_lower_DisplayName"";");

            migrationBuilder.DropIndex(
                name: "IX_Players_RoomId",
                table: "Players");

            migrationBuilder.CreateIndex(
                name: "IX_Players_RoomId_DisplayName",
                table: "Players",
                columns: new[] { "RoomId", "DisplayName" },
                unique: true);
        }
    }
}
