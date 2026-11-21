using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizBattle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PracticeSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoomCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    RoomTopic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QuestionIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeSessions_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PracticeSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticeXpDaily",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Xp = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeXpDaily", x => new { x.UserId, x.Day });
                    table.CheckConstraint("CK_PracticeXpDaily_Xp_NonNegative", "\"Xp\" >= 0");
                    table.ForeignKey(
                        name: "FK_PracticeXpDaily_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticeAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SelectedIndex = table.Column<int>(type: "integer", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    XpAwarded = table.Column<int>(type: "integer", nullable: false),
                    XpReason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeAnswers", x => x.Id);
                    table.CheckConstraint("CK_PracticeAnswers_SelectedIndex_Range", "\"SelectedIndex\" BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_PracticeAnswers_XpAwarded_NonNegative", "\"XpAwarded\" >= 0");
                    table.CheckConstraint("CK_PracticeAnswers_XpReason", "(\"XpReason\" IS NULL AND \"IsCorrect\") OR (\"XpReason\" IS NOT NULL AND ((\"XpReason\" = 'wrong' AND NOT \"IsCorrect\" AND \"XpAwarded\" = 0) OR (\"XpReason\" IN ('already_today', 'daily_cap') AND \"IsCorrect\" AND \"XpAwarded\" = 0) OR (\"XpReason\" = 'daily_cap_partial' AND \"IsCorrect\" AND \"XpAwarded\" > 0)))");
                    table.ForeignKey(
                        name: "FK_PracticeAnswers_PracticeSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "PracticeSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PracticeAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PracticeAnswers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeAnswers_QuestionId",
                table: "PracticeAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeAnswers_SessionId_QuestionId",
                table: "PracticeAnswers",
                columns: new[] { "SessionId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticeAnswers_UserId_QuestionId_Day_Awarded",
                table: "PracticeAnswers",
                columns: new[] { "UserId", "QuestionId", "Day" },
                unique: true,
                filter: "\"XpAwarded\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSessions_OpenPerUserRoom",
                table: "PracticeSessions",
                columns: new[] { "UserId", "RoomId" },
                unique: true,
                filter: "\"CompletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSessions_RoomId",
                table: "PracticeSessions",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSessions_UserId_StartedAt",
                table: "PracticeSessions",
                columns: new[] { "UserId", "StartedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PracticeAnswers");

            migrationBuilder.DropTable(
                name: "PracticeXpDaily");

            migrationBuilder.DropTable(
                name: "PracticeSessions");
        }
    }
}
