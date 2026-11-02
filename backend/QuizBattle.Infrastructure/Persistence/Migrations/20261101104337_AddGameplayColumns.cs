using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizBattle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameplayColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                table: "Questions",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Players",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_UserId",
                table: "Players",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Users_UserId",
                table: "Players",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Users_UserId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_UserId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Players");
        }
    }
}
