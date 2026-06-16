using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivacyIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrivacyIncidentId",
                table: "EvidenceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PrivacyIncidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IncidentNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    OwnRole = table.Column<int>(type: "int", nullable: false),
                    DiscoveredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReportedToUsAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponsiblePerson = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InternalReference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HowDetected = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cause = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AffectedSystems = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IncidentStillActive = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    IncidentStoppedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConfidentialityAffected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IntegrityAffected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AvailabilityAffected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BreachTypeDescription = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AffectedDataCategories = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AffectedPersonGroups = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApproxAffectedPersons = table.Column<int>(type: "int", nullable: true),
                    ApproxAffectedRecords = table.Column<int>(type: "int", nullable: true),
                    SpecialCategoriesAffected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LikelyConsequences = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RiskLevel = table.Column<int>(type: "int", nullable: false),
                    RiskAssessmentReason = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SupervisoryAuthorityNotificationRequired = table.Column<int>(type: "int", nullable: false),
                    SupervisoryAuthorityNotificationReason = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SupervisoryAuthorityName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SupervisoryAuthorityNotifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SupervisoryAuthorityReference = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NotificationDelayReason = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataSubjectsNotificationRequired = table.Column<int>(type: "int", nullable: false),
                    DataSubjectsNotificationReason = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataSubjectsNotifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DataSubjectsNotificationMethod = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataSubjectsNotificationSummary = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImmediateActions = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RemediationActions = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreventiveActions = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClosureSummary = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClosedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsArchived = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ArchivedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivacyIncidents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PrivacyIncidentMeasures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PrivacyIncidentId = table.Column<int>(type: "int", nullable: false),
                    MeasureId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyIncidentMeasures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivacyIncidentMeasures_Measures_MeasureId",
                        column: x => x.MeasureId,
                        principalTable: "Measures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrivacyIncidentMeasures_PrivacyIncidents_PrivacyIncidentId",
                        column: x => x.PrivacyIncidentId,
                        principalTable: "PrivacyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrivacyIncidentMeasures_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PrivacyIncidentProcessingActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PrivacyIncidentId = table.Column<int>(type: "int", nullable: false),
                    ProcessingActivityId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyIncidentProcessingActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivacyIncidentProcessingActivities_PrivacyIncidents_Privacy~",
                        column: x => x.PrivacyIncidentId,
                        principalTable: "PrivacyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrivacyIncidentProcessingActivities_ProcessingActivities_Pro~",
                        column: x => x.ProcessingActivityId,
                        principalTable: "ProcessingActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrivacyIncidentProcessingActivities_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PrivacyIncidentServiceProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PrivacyIncidentId = table.Column<int>(type: "int", nullable: false),
                    ServiceProviderId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyIncidentServiceProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivacyIncidentServiceProviders_PrivacyIncidents_PrivacyInci~",
                        column: x => x.PrivacyIncidentId,
                        principalTable: "PrivacyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrivacyIncidentServiceProviders_ServiceProviders_ServiceProv~",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrivacyIncidentServiceProviders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_PrivacyIncidentId",
                table: "EvidenceDocuments",
                column: "PrivacyIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_TenantId_PrivacyIncidentId",
                table: "EvidenceDocuments",
                columns: new[] { "TenantId", "PrivacyIncidentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidentMeasures_MeasureId",
                table: "PrivacyIncidentMeasures",
                column: "MeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidentMeasures_PrivacyIncidentId_MeasureId",
                table: "PrivacyIncidentMeasures",
                columns: new[] { "PrivacyIncidentId", "MeasureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidentMeasures_TenantId",
                table: "PrivacyIncidentMeasures",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidentProcessingActivities_PrivacyIncidentId_Proces~",
                table: "PrivacyIncidentProcessingActivities",
                columns: new[] { "PrivacyIncidentId", "ProcessingActivityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidentProcessingActivities_ProcessingActivityId",
                table: "PrivacyIncidentProcessingActivities",
                column: "ProcessingActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidentProcessingActivities_TenantId",
                table: "PrivacyIncidentProcessingActivities",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidents_TenantId",
                table: "PrivacyIncidents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidents_TenantId_IncidentNumber",
                table: "PrivacyIncidents",
                columns: new[] { "TenantId", "IncidentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidents_TenantId_Status",
                table: "PrivacyIncidents",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidentServiceProviders_PrivacyIncidentId_ServicePro~",
                table: "PrivacyIncidentServiceProviders",
                columns: new[] { "PrivacyIncidentId", "ServiceProviderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidentServiceProviders_ServiceProviderId",
                table: "PrivacyIncidentServiceProviders",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidentServiceProviders_TenantId",
                table: "PrivacyIncidentServiceProviders",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceDocuments_PrivacyIncidents_PrivacyIncidentId",
                table: "EvidenceDocuments",
                column: "PrivacyIncidentId",
                principalTable: "PrivacyIncidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceDocuments_PrivacyIncidents_PrivacyIncidentId",
                table: "EvidenceDocuments");

            migrationBuilder.DropTable(
                name: "PrivacyIncidentMeasures");

            migrationBuilder.DropTable(
                name: "PrivacyIncidentProcessingActivities");

            migrationBuilder.DropTable(
                name: "PrivacyIncidentServiceProviders");

            migrationBuilder.DropTable(
                name: "PrivacyIncidents");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceDocuments_PrivacyIncidentId",
                table: "EvidenceDocuments");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceDocuments_TenantId_PrivacyIncidentId",
                table: "EvidenceDocuments");

            migrationBuilder.DropColumn(
                name: "PrivacyIncidentId",
                table: "EvidenceDocuments");
        }
    }
}
