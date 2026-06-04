using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddToms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Toms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<int>(type: "int", nullable: false),
                    ProtectionGoal = table.Column<int>(type: "int", nullable: false),
                    ImplementationStatus = table.Column<int>(type: "int", nullable: false),
                    Owner = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    NextReviewAt = table.Column<DateOnly>(type: "date", nullable: true),
                    EvidenceReference = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Toms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Toms_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProcessingActivityToms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ProcessingActivityId = table.Column<int>(type: "int", nullable: false),
                    TomId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingActivityToms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingActivityToms_ProcessingActivities_ProcessingActivi~",
                        column: x => x.ProcessingActivityId,
                        principalTable: "ProcessingActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcessingActivityToms_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessingActivityToms_Toms_TomId",
                        column: x => x.TomId,
                        principalTable: "Toms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingActivityToms_ProcessingActivityId",
                table: "ProcessingActivityToms",
                column: "ProcessingActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingActivityToms_TenantId",
                table: "ProcessingActivityToms",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingActivityToms_TomId_ProcessingActivityId",
                table: "ProcessingActivityToms",
                columns: new[] { "TomId", "ProcessingActivityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Toms_TenantId",
                table: "Toms",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Toms_TenantId_ImplementationStatus",
                table: "Toms",
                columns: new[] { "TenantId", "ImplementationStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessingActivityToms");

            migrationBuilder.DropTable(
                name: "Toms");
        }
    }
}
