using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingActivityRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropIndex(
            //    name: "IX_EvidenceDocuments_TenantId",
            //    table: "EvidenceDocuments");

            migrationBuilder.AddColumn<int>(
                name: "ProcessingActivityId",
                table: "EvidenceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProcessingActivityAuditAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ProcessingActivityId = table.Column<int>(type: "int", nullable: false),
                    AuditAnswerId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingActivityAuditAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingActivityAuditAnswers_AuditAnswers_AuditAnswerId",
                        column: x => x.AuditAnswerId,
                        principalTable: "AuditAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcessingActivityAuditAnswers_ProcessingActivities_Processi~",
                        column: x => x.ProcessingActivityId,
                        principalTable: "ProcessingActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcessingActivityAuditAnswers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProcessingActivityMeasures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ProcessingActivityId = table.Column<int>(type: "int", nullable: false),
                    MeasureId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingActivityMeasures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingActivityMeasures_Measures_MeasureId",
                        column: x => x.MeasureId,
                        principalTable: "Measures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcessingActivityMeasures_ProcessingActivities_ProcessingAc~",
                        column: x => x.ProcessingActivityId,
                        principalTable: "ProcessingActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcessingActivityMeasures_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_ProcessingActivityId",
                table: "EvidenceDocuments",
                column: "ProcessingActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_TenantId_ProcessingActivityId",
                table: "EvidenceDocuments",
                columns: new[] { "TenantId", "ProcessingActivityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingActivityAuditAnswers_AuditAnswerId",
                table: "ProcessingActivityAuditAnswers",
                column: "AuditAnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingActivityAuditAnswers_ProcessingActivityId_AuditAns~",
                table: "ProcessingActivityAuditAnswers",
                columns: new[] { "ProcessingActivityId", "AuditAnswerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingActivityAuditAnswers_TenantId",
                table: "ProcessingActivityAuditAnswers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingActivityMeasures_MeasureId",
                table: "ProcessingActivityMeasures",
                column: "MeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingActivityMeasures_ProcessingActivityId_MeasureId",
                table: "ProcessingActivityMeasures",
                columns: new[] { "ProcessingActivityId", "MeasureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingActivityMeasures_TenantId",
                table: "ProcessingActivityMeasures",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceDocuments_ProcessingActivities_ProcessingActivityId",
                table: "EvidenceDocuments",
                column: "ProcessingActivityId",
                principalTable: "ProcessingActivities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceDocuments_ProcessingActivities_ProcessingActivityId",
                table: "EvidenceDocuments");

            migrationBuilder.DropTable(
                name: "ProcessingActivityAuditAnswers");

            migrationBuilder.DropTable(
                name: "ProcessingActivityMeasures");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceDocuments_ProcessingActivityId",
                table: "EvidenceDocuments");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceDocuments_TenantId_ProcessingActivityId",
                table: "EvidenceDocuments");

            migrationBuilder.DropColumn(
                name: "ProcessingActivityId",
                table: "EvidenceDocuments");

            //migrationBuilder.CreateIndex(
            //    name: "IX_EvidenceDocuments_TenantId",
            //    table: "EvidenceDocuments",
            //    column: "TenantId");
        }
    }
}
