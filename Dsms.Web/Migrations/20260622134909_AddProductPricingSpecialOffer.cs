using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPricingSpecialOffer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SpecialBaseMonthlyPrice",
                table: "ProductPricingSettings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpecialBaseYearlyPrice",
                table: "ProductPricingSettings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SpecialOfferActive",
                table: "ProductPricingSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SpecialOfferBadgeText",
                table: "ProductPricingSettings",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpecialBaseMonthlyPrice",
                table: "ProductPricingSettings");

            migrationBuilder.DropColumn(
                name: "SpecialBaseYearlyPrice",
                table: "ProductPricingSettings");

            migrationBuilder.DropColumn(
                name: "SpecialOfferActive",
                table: "ProductPricingSettings");

            migrationBuilder.DropColumn(
                name: "SpecialOfferBadgeText",
                table: "ProductPricingSettings");
        }
    }
}
