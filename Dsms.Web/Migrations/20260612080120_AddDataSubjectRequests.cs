using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDataSubjectRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataSubjectRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RequestType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DataSubjectName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataSubjectEmail = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataSubjectPhone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataSubjectReference = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactChannel = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultSummary = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InternalNotes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssignedUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdentityVerified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IdentityVerificationNote = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeadlineExtended = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeadlineExtensionReason = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExtendedDueAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ContainsPersonalData = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PersonalDataAnonymized = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PersonalDataAnonymizedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PersonalDataAnonymizedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnonymizationNote = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_DataSubjectRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequests_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DataSubjectRequestMeasures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DataSubjectRequestId = table.Column<int>(type: "int", nullable: false),
                    MeasureId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSubjectRequestMeasures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequestMeasures_DataSubjectRequests_DataSubjectRe~",
                        column: x => x.DataSubjectRequestId,
                        principalTable: "DataSubjectRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequestMeasures_Measures_MeasureId",
                        column: x => x.MeasureId,
                        principalTable: "Measures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequestMeasures_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DataSubjectRequestProcessingActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DataSubjectRequestId = table.Column<int>(type: "int", nullable: false),
                    ProcessingActivityId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSubjectRequestProcessingActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequestProcessingActivities_DataSubjectRequests_D~",
                        column: x => x.DataSubjectRequestId,
                        principalTable: "DataSubjectRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequestProcessingActivities_ProcessingActivities_~",
                        column: x => x.ProcessingActivityId,
                        principalTable: "ProcessingActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequestProcessingActivities_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DataSubjectRequestServiceProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DataSubjectRequestId = table.Column<int>(type: "int", nullable: false),
                    ServiceProviderId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSubjectRequestServiceProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequestServiceProviders_DataSubjectRequests_DataS~",
                        column: x => x.DataSubjectRequestId,
                        principalTable: "DataSubjectRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequestServiceProviders_ServiceProviders_ServiceP~",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequestServiceProviders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequestMeasures_DataSubjectRequestId",
                table: "DataSubjectRequestMeasures",
                column: "DataSubjectRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequestMeasures_DataSubjectRequestId_MeasureId",
                table: "DataSubjectRequestMeasures",
                columns: new[] { "DataSubjectRequestId", "MeasureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequestMeasures_MeasureId",
                table: "DataSubjectRequestMeasures",
                column: "MeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequestMeasures_TenantId",
                table: "DataSubjectRequestMeasures",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequestProcessingActivities_DataSubjectRequestId",
                table: "DataSubjectRequestProcessingActivities",
                column: "DataSubjectRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequestProcessingActivities_DataSubjectRequestId_~",
                table: "DataSubjectRequestProcessingActivities",
                columns: new[] { "DataSubjectRequestId", "ProcessingActivityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequestProcessingActivities_ProcessingActivityId",
                table: "DataSubjectRequestProcessingActivities",
                column: "ProcessingActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequestProcessingActivities_TenantId",
                table: "DataSubjectRequestProcessingActivities",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_DueAt",
                table: "DataSubjectRequests",
                column: "DueAt");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_PersonalDataAnonymized",
                table: "DataSubjectRequests",
                column: "PersonalDataAnonymized");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_RequestType",
                table: "DataSubjectRequests",
                column: "RequestType");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_Status",
                table: "DataSubjectRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_TenantId",
                table: "DataSubjectRequests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_TenantId_DueAt",
                table: "DataSubjectRequests",
                columns: new[] { "TenantId", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_TenantId_Status",
                table: "DataSubjectRequests",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequestServiceProviders_DataSubjectRequestId",
                table: "DataSubjectRequestServiceProviders",
                column: "DataSubjectRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequestServiceProviders_DataSubjectRequestId_Serv~",
                table: "DataSubjectRequestServiceProviders",
                columns: new[] { "DataSubjectRequestId", "ServiceProviderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequestServiceProviders_ServiceProviderId",
                table: "DataSubjectRequestServiceProviders",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequestServiceProviders_TenantId",
                table: "DataSubjectRequestServiceProviders",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataSubjectRequestMeasures");

            migrationBuilder.DropTable(
                name: "DataSubjectRequestProcessingActivities");

            migrationBuilder.DropTable(
                name: "DataSubjectRequestServiceProviders");

            migrationBuilder.DropTable(
                name: "DataSubjectRequests");
        }
    }
}
