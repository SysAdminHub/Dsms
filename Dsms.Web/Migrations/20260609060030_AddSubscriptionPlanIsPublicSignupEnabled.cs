using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanIsPublicSignupEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublicSignupEnabled",
                table: "SubscriptionPlans",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE SubscriptionPlans
                SET IsPublicSignupEnabled = 1
                WHERE IsActive = 1 AND IsFree = 1
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublicSignupEnabled",
                table: "SubscriptionPlans");
        }
    }
}
