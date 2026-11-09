using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizBattle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddXpCoinsAndPlayerAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CoinBalance",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalXp",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MatchRewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoomCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    RoomTopic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DenialReason = table.Column<string>(type: "text", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    RewardRank = table.Column<int>(type: "integer", nullable: false),
                    Xp = table.Column<int>(type: "integer", nullable: false),
                    Coins = table.Column<int>(type: "integer", nullable: false),
                    RankBonus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchRewards", x => x.Id);
                    table.CheckConstraint("CK_MatchRewards_Amounts_NonNegative", "\"Xp\" >= 0 AND \"Coins\" >= 0 AND \"RankBonus\" >= 0");
                    table.CheckConstraint("CK_MatchRewards_DenialReason_MatchesStatus", "(\"Status\" = 'Awarded') = (\"DenialReason\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_MatchRewards_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MatchRewards_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MatchRewards_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoinTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchRewardId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoinTransactions", x => x.Id);
                    table.CheckConstraint("CK_CoinTransactions_Amount_NonZero", "\"Amount\" <> 0");
                    table.ForeignKey(
                        name: "FK_CoinTransactions_MatchRewards_MatchRewardId",
                        column: x => x.MatchRewardId,
                        principalTable: "MatchRewards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CoinTransactions_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CoinTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TotalXp",
                table: "Users",
                columns: new[] { "TotalXp", "CreatedAt", "Id" },
                descending: new[] { true, false, false },
                filter: "\"TotalXp\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_CoinBalance_NonNegative",
                table: "Users",
                sql: "\"CoinBalance\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_TotalXp_NonNegative",
                table: "Users",
                sql: "\"TotalXp\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Players_RoomId_UserId",
                table: "Players",
                columns: new[] { "RoomId", "UserId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_MatchRewardId",
                table: "CoinTransactions",
                column: "MatchRewardId",
                unique: true,
                filter: "\"MatchRewardId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_RoomId",
                table: "CoinTransactions",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_UserId_CreatedAt",
                table: "CoinTransactions",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_MatchRewards_PlayerId",
                table: "MatchRewards",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchRewards_RoomId",
                table: "MatchRewards",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRewards_UserId_FinishedAt",
                table: "MatchRewards",
                columns: new[] { "UserId", "FinishedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoinTransactions");

            migrationBuilder.DropTable(
                name: "MatchRewards");

            migrationBuilder.DropIndex(
                name: "IX_Users_TotalXp",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_CoinBalance_NonNegative",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_TotalXp_NonNegative",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Players_RoomId_UserId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CoinBalance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TotalXp",
                table: "Users");
        }
    }
}
