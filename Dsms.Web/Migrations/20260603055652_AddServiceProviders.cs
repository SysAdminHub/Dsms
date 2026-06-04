using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServiceProviderId",
                table: "EvidenceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderType = table.Column<int>(type: "int", nullable: false),
                    ServicePurpose = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactPerson = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Website = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Country = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDataProcessor = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataProcessingAgreementExists = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataProcessingAgreementDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DataProcessingAgreementReviewedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    DataProcessingAgreementReviewResult = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TomsReviewed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TomsReviewedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    TomsReviewResult = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubProcessorsAllowed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SubProcessorsDescription = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThirdCountryInvolvement = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ThirdCountry = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThirdCountryLegalBasis = table.Column<int>(type: "int", nullable: false),
                    ThirdCountryTransferGuarantees = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RiskAssessment = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResponsiblePerson = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProviders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProcessingActivityServiceProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ProcessingActivityId = table.Column<int>(type: "int", nullable: false),
                    ServiceProviderId = table.Column<int>(type: "int", nullable: false),
                    RoleInProcessing = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingActivityServiceProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingActivityServiceProviders_ProcessingActivities_Proc~",
                        column: x => x.ProcessingActivityId,
                        principalTable: "ProcessingActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcessingActivityServiceProviders_ServiceProviders_ServiceP~",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcessingActivityServiceProviders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServiceProviderToms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ServiceProviderId = table.Column<int>(type: "int", nullable: false),
                    TomId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProviderToms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProviderToms_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceProviderToms_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceProviderToms_Toms_TomId",
                        column: x => x.TomId,
                        principalTable: "Toms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_ServiceProviderId",
                table: "EvidenceDocuments",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingActivityServiceProviders_ProcessingActivityId",
                table: "ProcessingActivityServiceProviders",
                column: "ProcessingActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingActivityServiceProviders_ServiceProviderId_Process~",
                table: "ProcessingActivityServiceProviders",
                columns: new[] { "ServiceProviderId", "ProcessingActivityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingActivityServiceProviders_TenantId",
                table: "ProcessingActivityServiceProviders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_TenantId",
                table: "ServiceProviders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_TenantId_IsDataProcessor",
                table: "ServiceProviders",
                columns: new[] { "TenantId", "IsDataProcessor" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_TenantId_Status",
                table: "ServiceProviders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderToms_ServiceProviderId_TomId",
                table: "ServiceProviderToms",
                columns: new[] { "ServiceProviderId", "TomId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderToms_TenantId",
                table: "ServiceProviderToms",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderToms_TomId",
                table: "ServiceProviderToms",
                column: "TomId");

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceDocuments_ServiceProviders_ServiceProviderId",
                table: "EvidenceDocuments",
                column: "ServiceProviderId",
                principalTable: "ServiceProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceDocuments_ServiceProviders_ServiceProviderId",
                table: "EvidenceDocuments");

            migrationBuilder.DropTable(
                name: "ProcessingActivityServiceProviders");

            migrationBuilder.DropTable(
                name: "ServiceProviderToms");

            migrationBuilder.DropTable(
                name: "ServiceProviders");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceDocuments_ServiceProviderId",
                table: "EvidenceDocuments");

            migrationBuilder.DropColumn(
                name: "ServiceProviderId",
                table: "EvidenceDocuments");
        }
    }
}
