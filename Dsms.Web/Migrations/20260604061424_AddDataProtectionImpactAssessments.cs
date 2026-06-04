using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDataProtectionImpactAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DataProtectionImpactAssessmentId",
                table: "EvidenceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DataProtectionImpactAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ProcessingActivityId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProcessingDescription = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReasonForDpia = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NecessityAndProportionality = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RiskAssessment = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProtectiveMeasures = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResidualRisk = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResponsiblePerson = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewedBy = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    NextReviewAt = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionImpactAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataProtectionImpactAssessments_ProcessingActivities_Process~",
                        column: x => x.ProcessingActivityId,
                        principalTable: "ProcessingActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataProtectionImpactAssessments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_DataProtectionImpactAssessmentId",
                table: "EvidenceDocuments",
                column: "DataProtectionImpactAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_TenantId_DataProtectionImpactAssessmentId",
                table: "EvidenceDocuments",
                columns: new[] { "TenantId", "DataProtectionImpactAssessmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_DataProtectionImpactAssessments_ProcessingActivityId",
                table: "DataProtectionImpactAssessments",
                column: "ProcessingActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProtectionImpactAssessments_TenantId",
                table: "DataProtectionImpactAssessments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProtectionImpactAssessments_TenantId_ProcessingActivityId",
                table: "DataProtectionImpactAssessments",
                columns: new[] { "TenantId", "ProcessingActivityId" });

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceDocuments_DataProtectionImpactAssessments_DataProtec~",
                table: "EvidenceDocuments",
                column: "DataProtectionImpactAssessmentId",
                principalTable: "DataProtectionImpactAssessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceDocuments_DataProtectionImpactAssessments_DataProtec~",
                table: "EvidenceDocuments");

            migrationBuilder.DropTable(
                name: "DataProtectionImpactAssessments");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceDocuments_DataProtectionImpactAssessmentId",
                table: "EvidenceDocuments");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceDocuments_TenantId_DataProtectionImpactAssessmentId",
                table: "EvidenceDocuments");

            migrationBuilder.DropColumn(
                name: "DataProtectionImpactAssessmentId",
                table: "EvidenceDocuments");
        }
    }
}
