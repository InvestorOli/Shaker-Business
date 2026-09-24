using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShakerBusiness.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdminBusinessEditLogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TimestampUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TargetTwitchUserId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetDisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdminTwitchUserId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdminDisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AppliedLive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BusinessId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OwnedBefore = table.Column<int>(type: "int", nullable: false),
                    OwnedAfter = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminBusinessEditLogEntries", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdminEditLogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TimestampUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TargetTwitchUserId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetDisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdminTwitchUserId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdminDisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AppliedLive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CashBefore = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CashAfter = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LifetimeEarningsBefore = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LifetimeEarningsAfter = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AngelCountBefore = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AngelCountAfter = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EarningsAtLastResetBefore = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EarningsAtLastResetAfter = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OliModifierPercentBefore = table.Column<double>(type: "double", nullable: false),
                    OliModifierPercentAfter = table.Column<double>(type: "double", nullable: false),
                    PrestigeCountBefore = table.Column<int>(type: "int", nullable: false),
                    PrestigeCountAfter = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminEditLogEntries", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdminRoleEditLogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TimestampUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TargetTwitchUserId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetDisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdminTwitchUserId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdminDisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AppliedLive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ModeratorBefore = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ModeratorAfter = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminRoleEditLogEntries", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FriendlyName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Xml = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GlobalGameStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    BoostMultiplier = table.Column<double>(type: "double", nullable: false),
                    BoostExpiresUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MaintenanceModeEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GameOverEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GameOverAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalGameStates", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerAccounts",
                columns: table => new
                {
                    TwitchUserId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProfileImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cash = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LifetimeEarnings = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EarningsAtLastReset = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AngelCount = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastOnlineUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsHidden = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsModerator = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDev = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsChatBanned = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OliChatRound = table.Column<int>(type: "int", nullable: false),
                    OliModifierPercent = table.Column<double>(type: "double", nullable: false),
                    SnakeHighScore = table.Column<int>(type: "int", nullable: false),
                    TimberHighScore = table.Column<int>(type: "int", nullable: false),
                    FishingXp = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    ResetCount = table.Column<int>(type: "int", nullable: false),
                    PrestigeCount = table.Column<int>(type: "int", nullable: false),
                    LastPrestigeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAccounts", x => x.TwitchUserId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerBaitInventoryEntries",
                columns: table => new
                {
                    TwitchUserId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BaitId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerBaitInventoryEntries", x => new { x.TwitchUserId, x.BaitId });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerFishCatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TwitchUserId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FishId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    CaughtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerFishCatches", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerFishDiscoveries",
                columns: table => new
                {
                    TwitchUserId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FishId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BestLevel = table.Column<int>(type: "int", nullable: false),
                    FirstCaughtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerFishDiscoveries", x => new { x.TwitchUserId, x.FishId });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerFishingQuests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TwitchUserId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FishId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Completed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BatchUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerFishingQuests", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ResetLogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TwitchUserId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimestampUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CashBefore = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LifetimeEarningsBefore = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EarningsAtLastResetBefore = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AngelCountBefore = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AngelsGained = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OliModifierPercentApplied = table.Column<double>(type: "double", nullable: false),
                    AngelCountAfter = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BusinessLevelsBeforeJson = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPrestige = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PrestigeCountAfter = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResetLogEntries", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerAngelUpgrades",
                columns: table => new
                {
                    TwitchUserId = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AngelUpgradeId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAngelUpgrades", x => new { x.TwitchUserId, x.AngelUpgradeId });
                    table.ForeignKey(
                        name: "FK_PlayerAngelUpgrades_PlayerAccounts_TwitchUserId",
                        column: x => x.TwitchUserId,
                        principalTable: "PlayerAccounts",
                        principalColumn: "TwitchUserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerBusinesses",
                columns: table => new
                {
                    TwitchUserId = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BusinessId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Owned = table.Column<int>(type: "int", nullable: false),
                    HasManager = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CycleAnchorUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerBusinesses", x => new { x.TwitchUserId, x.BusinessId });
                    table.ForeignKey(
                        name: "FK_PlayerBusinesses_PlayerAccounts_TwitchUserId",
                        column: x => x.TwitchUserId,
                        principalTable: "PlayerAccounts",
                        principalColumn: "TwitchUserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerFishingProfiles",
                columns: table => new
                {
                    TwitchUserId = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Pearls = table.Column<long>(type: "bigint", nullable: false, defaultValue: 100L),
                    RodLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ActiveBaitId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerFishingProfiles", x => x.TwitchUserId);
                    table.ForeignKey(
                        name: "FK_PlayerFishingProfiles_PlayerAccounts_TwitchUserId",
                        column: x => x.TwitchUserId,
                        principalTable: "PlayerAccounts",
                        principalColumn: "TwitchUserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerUpgrades",
                columns: table => new
                {
                    TwitchUserId = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpgradeId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerUpgrades", x => new { x.TwitchUserId, x.UpgradeId });
                    table.ForeignKey(
                        name: "FK_PlayerUpgrades_PlayerAccounts_TwitchUserId",
                        column: x => x.TwitchUserId,
                        principalTable: "PlayerAccounts",
                        principalColumn: "TwitchUserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AdminBusinessEditLogEntries_TargetTwitchUserId",
                table: "AdminBusinessEditLogEntries",
                column: "TargetTwitchUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminBusinessEditLogEntries_TimestampUtc",
                table: "AdminBusinessEditLogEntries",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdminEditLogEntries_TargetTwitchUserId",
                table: "AdminEditLogEntries",
                column: "TargetTwitchUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminEditLogEntries_TimestampUtc",
                table: "AdminEditLogEntries",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRoleEditLogEntries_TargetTwitchUserId",
                table: "AdminRoleEditLogEntries",
                column: "TargetTwitchUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRoleEditLogEntries_TimestampUtc",
                table: "AdminRoleEditLogEntries",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFishCatches_TwitchUserId_FishId",
                table: "PlayerFishCatches",
                columns: new[] { "TwitchUserId", "FishId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFishingQuests_TwitchUserId",
                table: "PlayerFishingQuests",
                column: "TwitchUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResetLogEntries_TimestampUtc",
                table: "ResetLogEntries",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ResetLogEntries_TwitchUserId",
                table: "ResetLogEntries",
                column: "TwitchUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminBusinessEditLogEntries");

            migrationBuilder.DropTable(
                name: "AdminEditLogEntries");

            migrationBuilder.DropTable(
                name: "AdminRoleEditLogEntries");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "GlobalGameStates");

            migrationBuilder.DropTable(
                name: "PlayerAngelUpgrades");

            migrationBuilder.DropTable(
                name: "PlayerBaitInventoryEntries");

            migrationBuilder.DropTable(
                name: "PlayerBusinesses");

            migrationBuilder.DropTable(
                name: "PlayerFishCatches");

            migrationBuilder.DropTable(
                name: "PlayerFishDiscoveries");

            migrationBuilder.DropTable(
                name: "PlayerFishingProfiles");

            migrationBuilder.DropTable(
                name: "PlayerFishingQuests");

            migrationBuilder.DropTable(
                name: "PlayerUpgrades");

            migrationBuilder.DropTable(
                name: "ResetLogEntries");

            migrationBuilder.DropTable(
                name: "PlayerAccounts");
        }
    }
}
