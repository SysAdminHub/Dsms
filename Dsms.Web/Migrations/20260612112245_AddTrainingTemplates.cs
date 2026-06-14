using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrainingType = table.Column<int>(type: "int", nullable: false),
                    TargetAudience = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    RecommendedRepeatAfterMonths = table.Column<int>(type: "int", nullable: true),
                    PassingScorePercent = table.Column<int>(type: "int", nullable: false, defaultValue: 80),
                    IsQuizRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsGlobal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsCommunityTemplate = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CommunityStatus = table.Column<int>(type: "int", nullable: false),
                    CommunitySubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CommunitySubmittedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommunityReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CommunityReviewedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommunityReviewNote = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsArchived = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ArchivedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingTemplates_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    TrainingTemplateId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    Explanation = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Points = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingQuestions_TrainingTemplates_TrainingTemplateId",
                        column: x => x.TrainingTemplateId,
                        principalTable: "TrainingTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingTemplateAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    TrainingTemplateId = table.Column<int>(type: "int", nullable: false),
                    AssetKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalFileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StoredFileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AltText = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingTemplateAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingTemplateAssets_TrainingTemplates_TrainingTemplateId",
                        column: x => x.TrainingTemplateId,
                        principalTable: "TrainingTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingTemplateSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    TrainingTemplateId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentMarkdown = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingTemplateSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingTemplateSections_TrainingTemplates_TrainingTemplateId",
                        column: x => x.TrainingTemplateId,
                        principalTable: "TrainingTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    TrainingQuestionId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    AnswerText = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Explanation = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingQuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingQuestionOptions_TrainingQuestions_TrainingQuestionId",
                        column: x => x.TrainingQuestionId,
                        principalTable: "TrainingQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuestionOptions_TenantId",
                table: "TrainingQuestionOptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuestionOptions_TrainingQuestionId",
                table: "TrainingQuestionOptions",
                column: "TrainingQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuestionOptions_TrainingQuestionId_SortOrder",
                table: "TrainingQuestionOptions",
                columns: new[] { "TrainingQuestionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuestions_TenantId",
                table: "TrainingQuestions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuestions_TrainingTemplateId",
                table: "TrainingQuestions",
                column: "TrainingTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuestions_TrainingTemplateId_SortOrder",
                table: "TrainingQuestions",
                columns: new[] { "TrainingTemplateId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplateAssets_AssetKey",
                table: "TrainingTemplateAssets",
                column: "AssetKey");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplateAssets_TenantId",
                table: "TrainingTemplateAssets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplateAssets_TrainingTemplateId",
                table: "TrainingTemplateAssets",
                column: "TrainingTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplateAssets_TrainingTemplateId_AssetKey",
                table: "TrainingTemplateAssets",
                columns: new[] { "TrainingTemplateId", "AssetKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplates_CommunityStatus",
                table: "TrainingTemplates",
                column: "CommunityStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplates_IsActive",
                table: "TrainingTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplates_IsGlobal",
                table: "TrainingTemplates",
                column: "IsGlobal");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplates_IsGlobal_IsActive",
                table: "TrainingTemplates",
                columns: new[] { "IsGlobal", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplates_TenantId",
                table: "TrainingTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplates_TenantId_IsActive",
                table: "TrainingTemplates",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplates_TrainingType",
                table: "TrainingTemplates",
                column: "TrainingType");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplateSections_TenantId",
                table: "TrainingTemplateSections",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplateSections_TrainingTemplateId",
                table: "TrainingTemplateSections",
                column: "TrainingTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplateSections_TrainingTemplateId_SortOrder",
                table: "TrainingTemplateSections",
                columns: new[] { "TrainingTemplateId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingQuestionOptions");

            migrationBuilder.DropTable(
                name: "TrainingTemplateAssets");

            migrationBuilder.DropTable(
                name: "TrainingTemplateSections");

            migrationBuilder.DropTable(
                name: "TrainingQuestions");

            migrationBuilder.DropTable(
                name: "TrainingTemplates");
        }
    }
}
