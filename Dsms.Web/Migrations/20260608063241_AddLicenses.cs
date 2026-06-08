using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LicenseId",
                table: "Tenants",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "LicenseId",
                table: "AspNetUsers",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "Licenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LicenseNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerEmail = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlanName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: "Manual")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "Active")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidFrom = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    InternalNote = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MaxTenants = table.Column<int>(type: "int", nullable: true),
                    MaxAdmins = table.Column<int>(type: "int", nullable: true),
                    MaxUsersPerTenant = table.Column<int>(type: "int", nullable: true),
                    MaxAuditorsPerTenant = table.Column<int>(type: "int", nullable: true),
                    MaxCustomAuditTemplatesPerTenant = table.Column<int>(type: "int", nullable: true),
                    MaxActiveAuditsPerTenant = table.Column<int>(type: "int", nullable: true),
                    MaxProcessingActivitiesPerTenant = table.Column<int>(type: "int", nullable: true),
                    MaxDpiaPerTenant = table.Column<int>(type: "int", nullable: true),
                    MaxTomsPerTenant = table.Column<int>(type: "int", nullable: true),
                    MaxProcessorsPerTenant = table.Column<int>(type: "int", nullable: true),
                    MaxActiveMeasuresPerTenant = table.Column<int>(type: "int", nullable: true),
                    MaxStorageMb = table.Column<int>(type: "int", nullable: true),
                    MaxEmailRemindersPerMonth = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licenses", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_LicenseId",
                table: "Tenants",
                column: "LicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LicenseId",
                table: "AspNetUsers",
                column: "LicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_CustomerName",
                table: "Licenses",
                column: "CustomerName");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_LicenseNumber",
                table: "Licenses",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_Status",
                table: "Licenses",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Licenses_LicenseId",
                table: "AspNetUsers",
                column: "LicenseId",
                principalTable: "Licenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_Licenses_LicenseId",
                table: "Tenants",
                column: "LicenseId",
                principalTable: "Licenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Licenses_LicenseId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_Licenses_LicenseId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "Licenses");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_LicenseId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LicenseId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LicenseId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LicenseId",
                table: "AspNetUsers");
        }
    }
}
