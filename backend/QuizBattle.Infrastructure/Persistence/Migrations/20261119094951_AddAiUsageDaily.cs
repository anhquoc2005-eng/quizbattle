using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizBattle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiUsageDaily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiUsageDaily",
                columns: table => new
                {
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUsageDaily", x => new { x.Day, x.Scope });
                    table.CheckConstraint("CK_AiUsageDaily_Count_NonNegative", "\"Count\" >= 0");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiUsageDaily");
        }
    }
}
