using Dsms.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <summary>
    /// Erweitert die globale Preis-/Produktkonfiguration (Tabelle ProductPricingSettings) um die
    /// Abrechnung zusätzlicher Mandanten: im Grundpreis enthaltene Mandantenanzahl sowie monatlicher
    /// und jährlicher Preis je zusätzlichem Mandanten. Bestehende (geseedete) Datensätze werden über
    /// die Default-Werte 1 / 9,00 / 99,00 sinnvoll vorbelegt.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260624091000_AddProductPricingAdditionalTenantPricing")]
    public partial class AddProductPricingAdditionalTenantPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IncludedTenantCount",
                table: "ProductPricingSettings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalTenantMonthlyPrice",
                table: "ProductPricingSettings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 9.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalTenantYearlyPrice",
                table: "ProductPricingSettings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 99.00m);

            // Bestehende aktive Preis-/Produktkonfiguration mit den fachlichen Defaultwerten versorgen,
            // falls die Spalten beim Hinzufügen mit 0 vorbelegt wurden (idempotent).
            migrationBuilder.Sql("""
                UPDATE ProductPricingSettings
                SET IncludedTenantCount = 1
                WHERE IncludedTenantCount = 0;
                """);

            migrationBuilder.Sql("""
                UPDATE ProductPricingSettings
                SET AdditionalTenantMonthlyPrice = 9.00
                WHERE AdditionalTenantMonthlyPrice = 0;
                """);

            migrationBuilder.Sql("""
                UPDATE ProductPricingSettings
                SET AdditionalTenantYearlyPrice = 99.00
                WHERE AdditionalTenantYearlyPrice = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludedTenantCount",
                table: "ProductPricingSettings");

            migrationBuilder.DropColumn(
                name: "AdditionalTenantMonthlyPrice",
                table: "ProductPricingSettings");

            migrationBuilder.DropColumn(
                name: "AdditionalTenantYearlyPrice",
                table: "ProductPricingSettings");
        }
    }
}
