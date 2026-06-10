using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivacyIncidentToms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrivacyIncidentToms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PrivacyIncidentId = table.Column<int>(type: "int", nullable: false),
                    TomId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyIncidentToms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivacyIncidentToms_PrivacyIncidents_PrivacyIncidentId",
                        column: x => x.PrivacyIncidentId,
                        principalTable: "PrivacyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrivacyIncidentToms_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrivacyIncidentToms_Toms_TomId",
                        column: x => x.TomId,
                        principalTable: "Toms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidentToms_PrivacyIncidentId_TomId",
                table: "PrivacyIncidentToms",
                columns: new[] { "PrivacyIncidentId", "TomId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidentToms_TenantId",
                table: "PrivacyIncidentToms",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyIncidentToms_TomId",
                table: "PrivacyIncidentToms",
                column: "TomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivacyIncidentToms");
        }
    }
}
