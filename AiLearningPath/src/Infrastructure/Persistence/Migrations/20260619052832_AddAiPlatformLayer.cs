using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiLearningPath.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiPlatformLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var largeText = ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase)
                ? "TEXT"
                : "nvarchar(max)";

            // Legacy SQLite databases may already have this column from EnsureCreated while
            // still missing the AI tables. Program.cs adds it after migrations only when absent.
            if (!ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.AddColumn<string>(
                    name: "SkillBreakdownJson",
                    table: "AssessmentResults",
                    type: largeText,
                    nullable: false,
                    defaultValue: "");
            }

            migrationBuilder.CreateTable(
                name: "AdaptationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningPathId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Summary = table.Column<string>(type: largeText, nullable: false),
                    AddedTasksJson = table.Column<string>(type: largeText, nullable: false),
                    FocusSkillsJson = table.Column<string>(type: largeText, nullable: false),
                    UsedFallback = table.Column<bool>(type: "bit", nullable: false),
                    FromCache = table.Column<bool>(type: "bit", nullable: false),
                    Provider = table.Column<string>(type: largeText, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdaptationEvents_LearningPaths_LearningPathId",
                        column: x => x.LearningPathId,
                        principalTable: "LearningPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdaptationEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiCacheEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CacheKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Operation = table.Column<string>(type: largeText, nullable: false),
                    SchemaVersion = table.Column<string>(type: largeText, nullable: false),
                    Content = table.Column<string>(type: largeText, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiCacheEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiFeedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: largeText, nullable: false),
                    TargetId = table.Column<string>(type: largeText, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: largeText, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiFeedback_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiInteractionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Operation = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FallbackReason = table.Column<string>(type: largeText, nullable: true),
                    LatencyMs = table.Column<long>(type: "bigint", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiInteractionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudySchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedFallback = table.Column<bool>(type: "bit", nullable: false),
                    FromCache = table.Column<bool>(type: "bit", nullable: false),
                    Provider = table.Column<string>(type: largeText, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudySchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudySchedules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TutorConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: largeText, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorConversations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyScheduleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Title = table.Column<string>(type: largeText, nullable: false),
                    Skill = table.Column<string>(type: largeText, nullable: false),
                    EstimatedHours = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyScheduleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyScheduleItems_StudySchedules_StudyScheduleId",
                        column: x => x.StudyScheduleId,
                        principalTable: "StudySchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TutorMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TutorConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: largeText, nullable: false),
                    Content = table.Column<string>(type: largeText, nullable: false),
                    Provider = table.Column<string>(type: largeText, nullable: false),
                    UsedFallback = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorMessages_TutorConversations_TutorConversationId",
                        column: x => x.TutorConversationId,
                        principalTable: "TutorConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdaptationEvents_LearningPathId",
                table: "AdaptationEvents",
                column: "LearningPathId");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptationEvents_UserId",
                table: "AdaptationEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiCacheEntries_CacheKey",
                table: "AiCacheEntries",
                column: "CacheKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiCacheEntries_ExpiresAt",
                table: "AiCacheEntries",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_AiFeedback_UserId",
                table: "AiFeedback",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiInteractionLogs_CorrelationId",
                table: "AiInteractionLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiInteractionLogs_UserId",
                table: "AiInteractionLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyScheduleItems_StudyScheduleId",
                table: "StudyScheduleItems",
                column: "StudyScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySchedules_UserId",
                table: "StudySchedules",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorConversations_UserId",
                table: "TutorConversations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorMessages_TutorConversationId",
                table: "TutorMessages",
                column: "TutorConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorMessages_UserId",
                table: "TutorMessages",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdaptationEvents");

            migrationBuilder.DropTable(
                name: "AiCacheEntries");

            migrationBuilder.DropTable(
                name: "AiFeedback");

            migrationBuilder.DropTable(
                name: "AiInteractionLogs");

            migrationBuilder.DropTable(
                name: "StudyScheduleItems");

            migrationBuilder.DropTable(
                name: "TutorMessages");

            migrationBuilder.DropTable(
                name: "StudySchedules");

            migrationBuilder.DropTable(
                name: "TutorConversations");

            if (!ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.DropColumn(
                    name: "SkillBreakdownJson",
                    table: "AssessmentResults");
            }
        }
    }
}
