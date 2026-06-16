using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    IsFree = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PriceMonthly = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PriceYearly = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "EUR")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalProductId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalMonthlyPriceId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalYearlyPriceId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InternalNote = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_IsActive",
                table: "SubscriptionPlans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_SortOrder",
                table: "SubscriptionPlans",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionPlans");
        }
    }
}
