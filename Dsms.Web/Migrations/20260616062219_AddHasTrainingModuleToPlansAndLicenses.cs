using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddHasTrainingModuleToPlansAndLicenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasTrainingModule",
                table: "SubscriptionPlans",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasTrainingModule",
                table: "Licenses",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql(
                """
                UPDATE SubscriptionPlans
                SET HasTrainingModule = 0
                WHERE Name IN ('free', 'basic')
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasTrainingModule",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "HasTrainingModule",
                table: "Licenses");
        }
    }
}
