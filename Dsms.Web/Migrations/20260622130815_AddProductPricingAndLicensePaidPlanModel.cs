using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPricingAndLicensePaidPlanModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdditionalStorageGb",
                table: "Licenses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IncludedStorageGb",
                table: "Licenses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LicensedUserCount",
                table: "Licenses",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "PaidPlanEnabled",
                table: "Licenses",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TrainingModuleStatus",
                table: "Licenses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrainingModuleTrialEndsAt",
                table: "Licenses",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrainingModuleUnlimited",
                table: "Licenses",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrainingModuleValidUntil",
                table: "Licenses",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductPricingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BaseMonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BaseYearlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AdditionalUserMonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AdditionalUserYearlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IncludedStorageGb = table.Column<int>(type: "int", nullable: false),
                    AdditionalStoragePackageGb = table.Column<int>(type: "int", nullable: false),
                    AdditionalStorageMonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AdditionalStorageYearlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LargeStoragePackageGb = table.Column<int>(type: "int", nullable: true),
                    LargeStorageMonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LargeStorageYearlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TrainingModuleMonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TrainingModuleYearlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TrainingTrialDays = table.Column<int>(type: "int", nullable: false),
                    FairUseText = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPricingSettings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPricingSettings_IsActive",
                table: "ProductPricingSettings",
                column: "IsActive");

            // Globale Standard-Preis-/Produktkonfiguration anlegen (nur, falls noch keine vorhanden ist).
            migrationBuilder.Sql("""
                INSERT INTO ProductPricingSettings
                    (BaseMonthlyPrice, BaseYearlyPrice,
                     AdditionalUserMonthlyPrice, AdditionalUserYearlyPrice,
                     IncludedStorageGb, AdditionalStoragePackageGb,
                     AdditionalStorageMonthlyPrice, AdditionalStorageYearlyPrice,
                     LargeStoragePackageGb, LargeStorageMonthlyPrice, LargeStorageYearlyPrice,
                     TrainingModuleMonthlyPrice, TrainingModuleYearlyPrice, TrainingTrialDays,
                     FairUseText, IsActive, CreatedAt)
                SELECT
                    29.00, 299.00,
                    5.00, 49.00,
                    10, 10,
                    5.00, 49.00,
                    50, 19.00, 199.00,
                    19.00, 199.00, 30,
                    'Im bezahlten Zugang sind Verarbeitungstätigkeiten, DSFAs, Maßnahmen, TOMs und Dienstleister grundsätzlich unbegrenzt enthalten. Die Nutzung erfolgt im Rahmen einer fairen und üblichen geschäftlichen Nutzung für Datenschutzmanagement. Bei ungewöhnlich hoher, missbräuchlicher oder zweckfremder Nutzung behalten wir uns vor, den Kunden zu kontaktieren und gemeinsam eine angemessene Lösung zu finden.',
                    1, UTC_TIMESTAMP()
                WHERE NOT EXISTS (SELECT 1 FROM ProductPricingSettings);
                """);

            // Bestehende Lizenzen sinnvoll mit Defaultwerten versorgen:
            // - im Grundpreis enthaltener Speicher (10 GB) als Basis, sobald später auf bezahlt umgestellt wird,
            // - Schulungsmodul-Legacy-Flag in den neuen Zustand überführen (an = dauerhaft / Unlimited).
            // PaidPlanEnabled bleibt bewusst 0 (Free-Verhalten bleibt unverändert, bestehende Limits gelten weiter).
            migrationBuilder.Sql("""
                UPDATE Licenses
                SET IncludedStorageGb = 10
                WHERE IncludedStorageGb = 0;
                """);

            migrationBuilder.Sql("""
                UPDATE Licenses
                SET TrainingModuleStatus = 3,
                    TrainingModuleUnlimited = 1
                WHERE HasTrainingModule = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductPricingSettings");

            migrationBuilder.DropColumn(
                name: "AdditionalStorageGb",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "IncludedStorageGb",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "LicensedUserCount",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "PaidPlanEnabled",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "TrainingModuleStatus",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "TrainingModuleTrialEndsAt",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "TrainingModuleUnlimited",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "TrainingModuleValidUntil",
                table: "Licenses");
        }
    }
}
