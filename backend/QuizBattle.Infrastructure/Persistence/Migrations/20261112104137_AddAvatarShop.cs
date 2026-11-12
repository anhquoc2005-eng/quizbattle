using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuizBattle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAvatarShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserAvatarItemId",
                table: "CoinTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AvatarItems",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Slot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VariantKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Tier = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarItems", x => x.Key);
                    table.CheckConstraint("CK_AvatarItems_Key", "\"Key\" = \"Slot\" || '.' || \"VariantKey\"");
                    table.CheckConstraint("CK_AvatarItems_Price", "(\"Tier\" = 'Free' AND \"Price\" = 0) OR (\"Tier\" <> 'Free' AND \"Price\" > 0)");
                });

            migrationBuilder.CreateTable(
                name: "UserAvatarItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AcquiredAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAvatarItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAvatarItems_AvatarItems_ItemKey",
                        column: x => x.ItemKey,
                        principalTable: "AvatarItems",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAvatarItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAvatars",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EyesItemKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EyebrowsItemKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MouthItemKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NoseItemKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    HairItemKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ClothesItemKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    GlassesItemKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    BeardItemKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    GestureItemKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ClothesGraphicItemKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    BackgroundItemKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAvatars", x => x.UserId);
                    table.CheckConstraint("CK_UserAvatars_background", "\"BackgroundItemKey\" IS NULL OR \"BackgroundItemKey\" LIKE 'background.%'");
                    table.CheckConstraint("CK_UserAvatars_beard", "\"BeardItemKey\" IS NULL OR \"BeardItemKey\" LIKE 'beard.%'");
                    table.CheckConstraint("CK_UserAvatars_clothes", "\"ClothesItemKey\" LIKE 'clothes.%'");
                    table.CheckConstraint("CK_UserAvatars_clothesGraphic", "\"ClothesGraphicItemKey\" IS NULL OR \"ClothesGraphicItemKey\" LIKE 'clothesGraphic.%'");
                    table.CheckConstraint("CK_UserAvatars_eyebrows", "\"EyebrowsItemKey\" LIKE 'eyebrows.%'");
                    table.CheckConstraint("CK_UserAvatars_eyes", "\"EyesItemKey\" LIKE 'eyes.%'");
                    table.CheckConstraint("CK_UserAvatars_gesture", "\"GestureItemKey\" IS NULL OR \"GestureItemKey\" LIKE 'gesture.%'");
                    table.CheckConstraint("CK_UserAvatars_glasses", "\"GlassesItemKey\" IS NULL OR \"GlassesItemKey\" LIKE 'glasses.%'");
                    table.CheckConstraint("CK_UserAvatars_hair", "\"HairItemKey\" LIKE 'hair.%'");
                    table.CheckConstraint("CK_UserAvatars_mouth", "\"MouthItemKey\" LIKE 'mouth.%'");
                    table.CheckConstraint("CK_UserAvatars_nose", "\"NoseItemKey\" LIKE 'nose.%'");
                    table.ForeignKey(
                        name: "FK_UserAvatars_AvatarItems_BackgroundItemKey",
                        column: x => x.BackgroundItemKey,
                        principalTable: "AvatarItems",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAvatars_AvatarItems_BeardItemKey",
                        column: x => x.BeardItemKey,
                        principalTable: "AvatarItems",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAvatars_AvatarItems_ClothesGraphicItemKey",
                        column: x => x.ClothesGraphicItemKey,
                        principalTable: "AvatarItems",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAvatars_AvatarItems_ClothesItemKey",
                        column: x => x.ClothesItemKey,
                        principalTable: "AvatarItems",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAvatars_AvatarItems_EyebrowsItemKey",
                        column: x => x.EyebrowsItemKey,
                        principalTable: "AvatarItems",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAvatars_AvatarItems_EyesItemKey",
                        column: x => x.EyesItemKey,
                        principalTable: "AvatarItems",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAvatars_AvatarItems_GestureItemKey",
                        column: x => x.GestureItemKey,
                        principalTable: "AvatarItems",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAvatars_AvatarItems_GlassesItemKey",
                        column: x => x.GlassesItemKey,
                        principalTable: "AvatarItems",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAvatars_AvatarItems_HairItemKey",
                        column: x => x.HairItemKey,
                        principalTable: "AvatarItems",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAvatars_AvatarItems_MouthItemKey",
                        column: x => x.MouthItemKey,
                        principalTable: "AvatarItems",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAvatars_AvatarItems_NoseItemKey",
                        column: x => x.NoseItemKey,
                        principalTable: "AvatarItems",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAvatars_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AvatarItems",
                columns: new[] { "Key", "Name", "Price", "Slot", "SortOrder", "Tier", "VariantKey" },
                values: new object[,]
                {
                    { "background.a9d3dc", "Xanh biển", 150, "background", 6, "Rare", "a9d3dc" },
                    { "background.b8d4be", "Xanh rêu", 50, "background", 2, "Common", "b8d4be" },
                    { "background.b9c6d6", "Xanh sương", 50, "background", 1, "Common", "b9c6d6" },
                    { "background.cfc4e3", "Tím oải hương", 50, "background", 5, "Common", "cfc4e3" },
                    { "background.d4d4d4", "Xám mặc định", 0, "background", 0, "Free", "d4d4d4" },
                    { "background.e3d5b8", "Cát", 50, "background", 3, "Common", "e3d5b8" },
                    { "background.e8c4c4", "Hồng bụi", 50, "background", 4, "Common", "e8c4c4" },
                    { "background.ebc4a0", "Đất nung", 150, "background", 7, "Rare", "ebc4a0" },
                    { "beard.variant01", "Râu 01", 50, "beard", 0, "Common", "variant01" },
                    { "beard.variant02", "Râu 02", 50, "beard", 1, "Common", "variant02" },
                    { "beard.variant03", "Râu 03", 50, "beard", 2, "Common", "variant03" },
                    { "beard.variant04", "Râu 04", 50, "beard", 3, "Common", "variant04" },
                    { "beard.variant05", "Râu 05", 50, "beard", 4, "Common", "variant05" },
                    { "beard.variant06", "Râu 06", 50, "beard", 5, "Common", "variant06" },
                    { "beard.variant07", "Râu 07", 150, "beard", 6, "Rare", "variant07" },
                    { "beard.variant08", "Râu 08", 150, "beard", 7, "Rare", "variant08" },
                    { "beard.variant09", "Râu 09", 150, "beard", 8, "Rare", "variant09" },
                    { "beard.variant10", "Râu 10", 150, "beard", 9, "Rare", "variant10" },
                    { "beard.variant11", "Râu 11", 150, "beard", 10, "Rare", "variant11" },
                    { "beard.variant12", "Râu 12", 150, "beard", 11, "Rare", "variant12" },
                    { "clothes.variant01", "Áo 01", 0, "clothes", 0, "Free", "variant01" },
                    { "clothes.variant02", "Áo 02", 0, "clothes", 1, "Free", "variant02" },
                    { "clothes.variant03", "Áo 03", 50, "clothes", 2, "Common", "variant03" },
                    { "clothes.variant04", "Áo 04", 50, "clothes", 3, "Common", "variant04" },
                    { "clothes.variant05", "Áo 05", 50, "clothes", 4, "Common", "variant05" },
                    { "clothes.variant06", "Áo 06", 50, "clothes", 5, "Common", "variant06" },
                    { "clothes.variant07", "Áo 07", 0, "clothes", 6, "Free", "variant07" },
                    { "clothes.variant08", "Áo 08", 50, "clothes", 7, "Common", "variant08" },
                    { "clothes.variant09", "Áo 09", 50, "clothes", 8, "Common", "variant09" },
                    { "clothes.variant10", "Áo 10", 50, "clothes", 9, "Common", "variant10" },
                    { "clothes.variant11", "Áo 11", 50, "clothes", 10, "Common", "variant11" },
                    { "clothes.variant12", "Áo 12", 50, "clothes", 11, "Common", "variant12" },
                    { "clothes.variant13", "Áo 13", 50, "clothes", 12, "Common", "variant13" },
                    { "clothes.variant14", "Áo 14", 0, "clothes", 13, "Free", "variant14" },
                    { "clothes.variant15", "Áo 15", 50, "clothes", 14, "Common", "variant15" },
                    { "clothes.variant16", "Áo 16", 50, "clothes", 15, "Common", "variant16" },
                    { "clothes.variant17", "Áo 17", 150, "clothes", 16, "Rare", "variant17" },
                    { "clothes.variant18", "Áo 18", 150, "clothes", 17, "Rare", "variant18" },
                    { "clothes.variant19", "Áo 19", 150, "clothes", 18, "Rare", "variant19" },
                    { "clothes.variant20", "Áo 20", 150, "clothes", 19, "Rare", "variant20" },
                    { "clothes.variant21", "Áo 21", 150, "clothes", 20, "Rare", "variant21" },
                    { "clothes.variant22", "Áo 22", 150, "clothes", 21, "Rare", "variant22" },
                    { "clothes.variant23", "Áo 23", 0, "clothes", 22, "Free", "variant23" },
                    { "clothes.variant24", "Áo 24", 150, "clothes", 23, "Rare", "variant24" },
                    { "clothes.variant25", "Áo 25", 150, "clothes", 24, "Rare", "variant25" },
                    { "clothesGraphic.electric", "Tia chớp", 400, "clothesGraphic", 0, "Special", "electric" },
                    { "clothesGraphic.galaxy", "Thiên hà", 400, "clothesGraphic", 1, "Special", "galaxy" },
                    { "clothesGraphic.saturn", "Sao Thổ", 400, "clothesGraphic", 2, "Special", "saturn" },
                    { "eyebrows.variant01", "Lông mày 01", 0, "eyebrows", 0, "Free", "variant01" },
                    { "eyebrows.variant02", "Lông mày 02", 0, "eyebrows", 1, "Free", "variant02" },
                    { "eyebrows.variant03", "Lông mày 03", 0, "eyebrows", 2, "Free", "variant03" },
                    { "eyebrows.variant04", "Lông mày 04", 0, "eyebrows", 3, "Free", "variant04" },
                    { "eyebrows.variant05", "Lông mày 05", 0, "eyebrows", 4, "Free", "variant05" },
                    { "eyebrows.variant06", "Lông mày 06", 0, "eyebrows", 5, "Free", "variant06" },
                    { "eyebrows.variant07", "Lông mày 07", 0, "eyebrows", 6, "Free", "variant07" },
                    { "eyebrows.variant08", "Lông mày 08", 0, "eyebrows", 7, "Free", "variant08" },
                    { "eyebrows.variant09", "Lông mày 09", 0, "eyebrows", 8, "Free", "variant09" },
                    { "eyebrows.variant10", "Lông mày 10", 0, "eyebrows", 9, "Free", "variant10" },
                    { "eyebrows.variant11", "Lông mày 11", 0, "eyebrows", 10, "Free", "variant11" },
                    { "eyebrows.variant12", "Lông mày 12", 0, "eyebrows", 11, "Free", "variant12" },
                    { "eyebrows.variant13", "Lông mày 13", 0, "eyebrows", 12, "Free", "variant13" },
                    { "eyes.variant01", "Mắt 01", 0, "eyes", 0, "Free", "variant01" },
                    { "eyes.variant02", "Mắt 02", 0, "eyes", 1, "Free", "variant02" },
                    { "eyes.variant03", "Mắt 03", 0, "eyes", 2, "Free", "variant03" },
                    { "eyes.variant04", "Mắt 04", 0, "eyes", 3, "Free", "variant04" },
                    { "eyes.variant05", "Mắt 05", 0, "eyes", 4, "Free", "variant05" },
                    { "gesture.hand", "Giơ tay", 150, "gesture", 0, "Rare", "hand" },
                    { "gesture.handPhone", "Cầm điện thoại", 150, "gesture", 1, "Rare", "handPhone" },
                    { "gesture.ok", "OK", 150, "gesture", 2, "Rare", "ok" },
                    { "gesture.okLongArm", "OK tay dài", 150, "gesture", 3, "Rare", "okLongArm" },
                    { "gesture.point", "Chỉ tay", 150, "gesture", 4, "Rare", "point" },
                    { "gesture.pointLongArm", "Chỉ tay dài", 150, "gesture", 5, "Rare", "pointLongArm" },
                    { "gesture.waveLongArm", "Vẫy tay dài", 150, "gesture", 6, "Rare", "waveLongArm" },
                    { "gesture.waveLongArms", "Vẫy hai tay dài", 150, "gesture", 7, "Rare", "waveLongArms" },
                    { "gesture.waveOkLongArms", "Vẫy tay và OK", 150, "gesture", 8, "Rare", "waveOkLongArms" },
                    { "gesture.wavePointLongArms", "Vẫy tay và chỉ", 150, "gesture", 9, "Rare", "wavePointLongArms" },
                    { "glasses.variant01", "Kính 01", 50, "glasses", 0, "Common", "variant01" },
                    { "glasses.variant02", "Kính 02", 50, "glasses", 1, "Common", "variant02" },
                    { "glasses.variant03", "Kính 03", 50, "glasses", 2, "Common", "variant03" },
                    { "glasses.variant04", "Kính 04", 50, "glasses", 3, "Common", "variant04" },
                    { "glasses.variant05", "Kính 05", 50, "glasses", 4, "Common", "variant05" },
                    { "glasses.variant06", "Kính 06", 50, "glasses", 5, "Common", "variant06" },
                    { "glasses.variant07", "Kính 07", 150, "glasses", 6, "Rare", "variant07" },
                    { "glasses.variant08", "Kính 08", 150, "glasses", 7, "Rare", "variant08" },
                    { "glasses.variant09", "Kính 09", 150, "glasses", 8, "Rare", "variant09" },
                    { "glasses.variant10", "Kính 10", 150, "glasses", 9, "Rare", "variant10" },
                    { "glasses.variant11", "Kính 11", 150, "glasses", 10, "Rare", "variant11" },
                    { "hair.hat", "Mũ", 400, "hair", 63, "Special", "hat" },
                    { "hair.variant01", "Tóc 01", 0, "hair", 0, "Free", "variant01" },
                    { "hair.variant02", "Tóc 02", 0, "hair", 1, "Free", "variant02" },
                    { "hair.variant03", "Tóc 03", 50, "hair", 2, "Common", "variant03" },
                    { "hair.variant04", "Tóc 04", 50, "hair", 3, "Common", "variant04" },
                    { "hair.variant05", "Tóc 05", 0, "hair", 4, "Free", "variant05" },
                    { "hair.variant06", "Tóc 06", 50, "hair", 5, "Common", "variant06" },
                    { "hair.variant07", "Tóc 07", 50, "hair", 6, "Common", "variant07" },
                    { "hair.variant08", "Tóc 08", 0, "hair", 7, "Free", "variant08" },
                    { "hair.variant09", "Tóc 09", 50, "hair", 8, "Common", "variant09" },
                    { "hair.variant10", "Tóc 10", 50, "hair", 9, "Common", "variant10" },
                    { "hair.variant11", "Tóc 11", 50, "hair", 10, "Common", "variant11" },
                    { "hair.variant12", "Tóc 12", 50, "hair", 11, "Common", "variant12" },
                    { "hair.variant13", "Tóc 13", 50, "hair", 12, "Common", "variant13" },
                    { "hair.variant14", "Tóc 14", 50, "hair", 13, "Common", "variant14" },
                    { "hair.variant15", "Tóc 15", 50, "hair", 14, "Common", "variant15" },
                    { "hair.variant16", "Tóc 16", 50, "hair", 15, "Common", "variant16" },
                    { "hair.variant17", "Tóc 17", 50, "hair", 16, "Common", "variant17" },
                    { "hair.variant18", "Tóc 18", 50, "hair", 17, "Common", "variant18" },
                    { "hair.variant19", "Tóc 19", 50, "hair", 18, "Common", "variant19" },
                    { "hair.variant20", "Tóc 20", 0, "hair", 19, "Free", "variant20" },
                    { "hair.variant21", "Tóc 21", 50, "hair", 20, "Common", "variant21" },
                    { "hair.variant22", "Tóc 22", 50, "hair", 21, "Common", "variant22" },
                    { "hair.variant23", "Tóc 23", 0, "hair", 22, "Free", "variant23" },
                    { "hair.variant24", "Tóc 24", 50, "hair", 23, "Common", "variant24" },
                    { "hair.variant25", "Tóc 25", 50, "hair", 24, "Common", "variant25" },
                    { "hair.variant26", "Tóc 26", 50, "hair", 25, "Common", "variant26" },
                    { "hair.variant27", "Tóc 27", 50, "hair", 26, "Common", "variant27" },
                    { "hair.variant28", "Tóc 28", 50, "hair", 27, "Common", "variant28" },
                    { "hair.variant29", "Tóc 29", 50, "hair", 28, "Common", "variant29" },
                    { "hair.variant30", "Tóc 30", 50, "hair", 29, "Common", "variant30" },
                    { "hair.variant31", "Tóc 31", 150, "hair", 30, "Rare", "variant31" },
                    { "hair.variant32", "Tóc 32", 150, "hair", 31, "Rare", "variant32" },
                    { "hair.variant33", "Tóc 33", 150, "hair", 32, "Rare", "variant33" },
                    { "hair.variant34", "Tóc 34", 150, "hair", 33, "Rare", "variant34" },
                    { "hair.variant35", "Tóc 35", 150, "hair", 34, "Rare", "variant35" },
                    { "hair.variant36", "Tóc 36", 150, "hair", 35, "Rare", "variant36" },
                    { "hair.variant37", "Tóc 37", 150, "hair", 36, "Rare", "variant37" },
                    { "hair.variant38", "Tóc 38", 150, "hair", 37, "Rare", "variant38" },
                    { "hair.variant39", "Tóc 39", 150, "hair", 38, "Rare", "variant39" },
                    { "hair.variant40", "Tóc 40", 150, "hair", 39, "Rare", "variant40" },
                    { "hair.variant41", "Tóc 41", 0, "hair", 40, "Free", "variant41" },
                    { "hair.variant42", "Tóc 42", 150, "hair", 41, "Rare", "variant42" },
                    { "hair.variant43", "Tóc 43", 150, "hair", 42, "Rare", "variant43" },
                    { "hair.variant44", "Tóc 44", 150, "hair", 43, "Rare", "variant44" },
                    { "hair.variant45", "Tóc 45", 0, "hair", 44, "Free", "variant45" },
                    { "hair.variant46", "Tóc 46", 150, "hair", 45, "Rare", "variant46" },
                    { "hair.variant47", "Tóc 47", 150, "hair", 46, "Rare", "variant47" },
                    { "hair.variant48", "Tóc 48", 150, "hair", 47, "Rare", "variant48" },
                    { "hair.variant49", "Tóc 49", 150, "hair", 48, "Rare", "variant49" },
                    { "hair.variant50", "Tóc 50", 150, "hair", 49, "Rare", "variant50" },
                    { "hair.variant51", "Tóc 51", 150, "hair", 50, "Rare", "variant51" },
                    { "hair.variant52", "Tóc 52", 150, "hair", 51, "Rare", "variant52" },
                    { "hair.variant53", "Tóc 53", 150, "hair", 52, "Rare", "variant53" },
                    { "hair.variant54", "Tóc 54", 400, "hair", 53, "Special", "variant54" },
                    { "hair.variant55", "Tóc 55", 400, "hair", 54, "Special", "variant55" },
                    { "hair.variant56", "Tóc 56", 400, "hair", 55, "Special", "variant56" },
                    { "hair.variant57", "Tóc 57", 400, "hair", 56, "Special", "variant57" },
                    { "hair.variant58", "Tóc 58", 400, "hair", 57, "Special", "variant58" },
                    { "hair.variant59", "Tóc 59", 400, "hair", 58, "Special", "variant59" },
                    { "hair.variant60", "Tóc 60", 400, "hair", 59, "Special", "variant60" },
                    { "hair.variant61", "Khăn quấn đầu", 0, "hair", 60, "Free", "variant61" },
                    { "hair.variant62", "Khăn trùm đầu 1", 0, "hair", 61, "Free", "variant62" },
                    { "hair.variant63", "Khăn trùm đầu 2", 0, "hair", 62, "Free", "variant63" },
                    { "mouth.variant01", "Miệng 01", 0, "mouth", 0, "Free", "variant01" },
                    { "mouth.variant02", "Miệng 02", 0, "mouth", 1, "Free", "variant02" },
                    { "mouth.variant03", "Miệng 03", 0, "mouth", 2, "Free", "variant03" },
                    { "mouth.variant04", "Miệng 04", 0, "mouth", 3, "Free", "variant04" },
                    { "mouth.variant05", "Miệng 05", 0, "mouth", 4, "Free", "variant05" },
                    { "mouth.variant06", "Miệng 06", 0, "mouth", 5, "Free", "variant06" },
                    { "mouth.variant07", "Miệng 07", 0, "mouth", 6, "Free", "variant07" },
                    { "mouth.variant08", "Miệng 08", 0, "mouth", 7, "Free", "variant08" },
                    { "mouth.variant09", "Miệng 09", 0, "mouth", 8, "Free", "variant09" },
                    { "mouth.variant10", "Miệng 10", 0, "mouth", 9, "Free", "variant10" },
                    { "mouth.variant11", "Miệng 11", 0, "mouth", 10, "Free", "variant11" },
                    { "mouth.variant12", "Miệng 12", 0, "mouth", 11, "Free", "variant12" },
                    { "mouth.variant13", "Miệng 13", 0, "mouth", 12, "Free", "variant13" },
                    { "mouth.variant14", "Miệng 14", 0, "mouth", 13, "Free", "variant14" },
                    { "mouth.variant15", "Miệng 15", 0, "mouth", 14, "Free", "variant15" },
                    { "mouth.variant16", "Miệng 16", 0, "mouth", 15, "Free", "variant16" },
                    { "mouth.variant17", "Miệng 17", 0, "mouth", 16, "Free", "variant17" },
                    { "mouth.variant18", "Miệng 18", 0, "mouth", 17, "Free", "variant18" },
                    { "mouth.variant19", "Miệng 19", 0, "mouth", 18, "Free", "variant19" },
                    { "mouth.variant20", "Miệng 20", 0, "mouth", 19, "Free", "variant20" },
                    { "mouth.variant21", "Miệng 21", 0, "mouth", 20, "Free", "variant21" },
                    { "mouth.variant22", "Miệng 22", 0, "mouth", 21, "Free", "variant22" },
                    { "mouth.variant23", "Miệng 23", 0, "mouth", 22, "Free", "variant23" },
                    { "mouth.variant24", "Miệng 24", 0, "mouth", 23, "Free", "variant24" },
                    { "mouth.variant25", "Miệng 25", 0, "mouth", 24, "Free", "variant25" },
                    { "mouth.variant26", "Miệng 26", 0, "mouth", 25, "Free", "variant26" },
                    { "mouth.variant27", "Miệng 27", 0, "mouth", 26, "Free", "variant27" },
                    { "mouth.variant28", "Miệng 28", 0, "mouth", 27, "Free", "variant28" },
                    { "mouth.variant29", "Miệng 29", 0, "mouth", 28, "Free", "variant29" },
                    { "mouth.variant30", "Miệng 30", 0, "mouth", 29, "Free", "variant30" },
                    { "nose.variant01", "Mũi 01", 0, "nose", 0, "Free", "variant01" },
                    { "nose.variant02", "Mũi 02", 0, "nose", 1, "Free", "variant02" },
                    { "nose.variant03", "Mũi 03", 0, "nose", 2, "Free", "variant03" },
                    { "nose.variant04", "Mũi 04", 0, "nose", 3, "Free", "variant04" },
                    { "nose.variant05", "Mũi 05", 0, "nose", 4, "Free", "variant05" },
                    { "nose.variant06", "Mũi 06", 0, "nose", 5, "Free", "variant06" },
                    { "nose.variant07", "Mũi 07", 0, "nose", 6, "Free", "variant07" },
                    { "nose.variant08", "Mũi 08", 0, "nose", 7, "Free", "variant08" },
                    { "nose.variant09", "Mũi 09", 0, "nose", 8, "Free", "variant09" },
                    { "nose.variant10", "Mũi 10", 0, "nose", 9, "Free", "variant10" },
                    { "nose.variant11", "Mũi 11", 0, "nose", 10, "Free", "variant11" },
                    { "nose.variant12", "Mũi 12", 0, "nose", 11, "Free", "variant12" },
                    { "nose.variant13", "Mũi 13", 0, "nose", 12, "Free", "variant13" },
                    { "nose.variant14", "Mũi 14", 0, "nose", 13, "Free", "variant14" },
                    { "nose.variant15", "Mũi 15", 0, "nose", 14, "Free", "variant15" },
                    { "nose.variant16", "Mũi 16", 0, "nose", 15, "Free", "variant16" },
                    { "nose.variant17", "Mũi 17", 0, "nose", 16, "Free", "variant17" },
                    { "nose.variant18", "Mũi 18", 0, "nose", 17, "Free", "variant18" },
                    { "nose.variant19", "Mũi 19", 0, "nose", 18, "Free", "variant19" },
                    { "nose.variant20", "Mũi 20", 0, "nose", 19, "Free", "variant20" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_UserAvatarItemId",
                table: "CoinTransactions",
                column: "UserAvatarItemId",
                unique: true,
                filter: "\"UserAvatarItemId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CoinTransactions_ReasonShape",
                table: "CoinTransactions",
                sql: "(\"Reason\" = 'MatchReward' AND \"Amount\" > 0 AND \"UserAvatarItemId\" IS NULL) OR (\"Reason\" = 'ShopPurchase' AND \"Amount\" < 0 AND \"UserAvatarItemId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_AvatarItems_Slot_VariantKey",
                table: "AvatarItems",
                columns: new[] { "Slot", "VariantKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatarItems_ItemKey",
                table: "UserAvatarItems",
                column: "ItemKey");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatarItems_UserId_ItemKey",
                table: "UserAvatarItems",
                columns: new[] { "UserId", "ItemKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_BackgroundItemKey",
                table: "UserAvatars",
                column: "BackgroundItemKey");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_BeardItemKey",
                table: "UserAvatars",
                column: "BeardItemKey");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_ClothesGraphicItemKey",
                table: "UserAvatars",
                column: "ClothesGraphicItemKey");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_ClothesItemKey",
                table: "UserAvatars",
                column: "ClothesItemKey");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_EyebrowsItemKey",
                table: "UserAvatars",
                column: "EyebrowsItemKey");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_EyesItemKey",
                table: "UserAvatars",
                column: "EyesItemKey");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_GestureItemKey",
                table: "UserAvatars",
                column: "GestureItemKey");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_GlassesItemKey",
                table: "UserAvatars",
                column: "GlassesItemKey");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_HairItemKey",
                table: "UserAvatars",
                column: "HairItemKey");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_MouthItemKey",
                table: "UserAvatars",
                column: "MouthItemKey");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_NoseItemKey",
                table: "UserAvatars",
                column: "NoseItemKey");

            migrationBuilder.AddForeignKey(
                name: "FK_CoinTransactions_UserAvatarItems_UserAvatarItemId",
                table: "CoinTransactions",
                column: "UserAvatarItemId",
                principalTable: "UserAvatarItems",
                principalColumn: "Id");

            // Bật RLS cho bảng mới (không tạo policy: vai trò anon/authenticated của Supabase Data API không đọc/ghi được gì;
            // backend kết nối bằng vai trò chủ bảng nên không bị ảnh hưởng).
            migrationBuilder.Sql("ALTER TABLE \"AvatarItems\" ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE \"UserAvatars\" ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE \"UserAvatarItems\" ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoinTransactions_UserAvatarItems_UserAvatarItemId",
                table: "CoinTransactions");

            migrationBuilder.DropTable(
                name: "UserAvatarItems");

            migrationBuilder.DropTable(
                name: "UserAvatars");

            migrationBuilder.DropTable(
                name: "AvatarItems");

            migrationBuilder.DropIndex(
                name: "IX_CoinTransactions_UserAvatarItemId",
                table: "CoinTransactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CoinTransactions_ReasonShape",
                table: "CoinTransactions");

            migrationBuilder.DropColumn(
                name: "UserAvatarItemId",
                table: "CoinTransactions");
        }
    }
}
