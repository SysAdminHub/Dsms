using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanPromotionalPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPromotionalPriceEnabled",
                table: "SubscriptionPlans",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PromotionalBadgeText",
                table: "SubscriptionPlans",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "PromotionalMonthlyPrice",
                table: "SubscriptionPlans",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PromotionalYearlyPrice",
                table: "SubscriptionPlans",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPromotionalPriceEnabled",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "PromotionalBadgeText",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "PromotionalMonthlyPrice",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "PromotionalYearlyPrice",
                table: "SubscriptionPlans");
        }
    }
}
