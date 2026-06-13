using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingParticipantPortalProgressAndQuiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessAtUtc",
                table: "TrainingAssignments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrainingAssignmentSectionProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TrainingAssignmentId = table.Column<int>(type: "int", nullable: false),
                    TrainingTemplateSectionId = table.Column<int>(type: "int", nullable: false),
                    ViewedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastViewedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    TimeSpentSeconds = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingAssignmentSectionProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingAssignmentSectionProgress_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingAssignmentSectionProgress_TrainingAssignments_Traini~",
                        column: x => x.TrainingAssignmentId,
                        principalTable: "TrainingAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingAssignmentSectionProgress_TrainingTemplateSections_T~",
                        column: x => x.TrainingTemplateSectionId,
                        principalTable: "TrainingTemplateSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingQuizAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TrainingAssignmentId = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ScorePercent = table.Column<int>(type: "int", nullable: false),
                    TotalPoints = table.Column<int>(type: "int", nullable: false),
                    AchievedPoints = table.Column<int>(type: "int", nullable: false),
                    Passed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingQuizAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingQuizAttempts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingQuizAttempts_TrainingAssignments_TrainingAssignmentId",
                        column: x => x.TrainingAssignmentId,
                        principalTable: "TrainingAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingQuizAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TrainingQuizAttemptId = table.Column<int>(type: "int", nullable: false),
                    TrainingQuestionId = table.Column<int>(type: "int", nullable: false),
                    TrainingQuestionOptionId = table.Column<int>(type: "int", nullable: true),
                    AnswerTextSnapshot = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSelected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PointsAwarded = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingQuizAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingQuizAnswers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingQuizAnswers_TrainingQuestionOptions_TrainingQuestion~",
                        column: x => x.TrainingQuestionOptionId,
                        principalTable: "TrainingQuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrainingQuizAnswers_TrainingQuestions_TrainingQuestionId",
                        column: x => x.TrainingQuestionId,
                        principalTable: "TrainingQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingQuizAnswers_TrainingQuizAttempts_TrainingQuizAttempt~",
                        column: x => x.TrainingQuizAttemptId,
                        principalTable: "TrainingQuizAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignmentSectionProgress_TenantId",
                table: "TrainingAssignmentSectionProgress",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignmentSectionProgress_TrainingAssignmentId",
                table: "TrainingAssignmentSectionProgress",
                column: "TrainingAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignmentSectionProgress_TrainingAssignmentId_Train~",
                table: "TrainingAssignmentSectionProgress",
                columns: new[] { "TrainingAssignmentId", "TrainingTemplateSectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignmentSectionProgress_TrainingTemplateSectionId",
                table: "TrainingAssignmentSectionProgress",
                column: "TrainingTemplateSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizAnswers_TenantId",
                table: "TrainingQuizAnswers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizAnswers_TrainingQuestionId",
                table: "TrainingQuizAnswers",
                column: "TrainingQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizAnswers_TrainingQuestionOptionId",
                table: "TrainingQuizAnswers",
                column: "TrainingQuestionOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizAnswers_TrainingQuizAttemptId",
                table: "TrainingQuizAnswers",
                column: "TrainingQuizAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizAttempts_AttemptNumber",
                table: "TrainingQuizAttempts",
                column: "AttemptNumber");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizAttempts_Passed",
                table: "TrainingQuizAttempts",
                column: "Passed");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizAttempts_SubmittedAtUtc",
                table: "TrainingQuizAttempts",
                column: "SubmittedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizAttempts_TenantId",
                table: "TrainingQuizAttempts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizAttempts_TrainingAssignmentId",
                table: "TrainingQuizAttempts",
                column: "TrainingAssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingAssignmentSectionProgress");

            migrationBuilder.DropTable(
                name: "TrainingQuizAnswers");

            migrationBuilder.DropTable(
                name: "TrainingQuizAttempts");

            migrationBuilder.DropColumn(
                name: "LastAccessAtUtc",
                table: "TrainingAssignments");
        }
    }
}
