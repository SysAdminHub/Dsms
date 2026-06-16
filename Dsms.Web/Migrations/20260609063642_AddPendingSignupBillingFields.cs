using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingSignupBillingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingCity",
                table: "PendingSignups",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BillingCompanyName",
                table: "PendingSignups",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BillingCountry",
                table: "PendingSignups",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BillingEmail",
                table: "PendingSignups",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BillingPostalCode",
                table: "PendingSignups",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BillingReference",
                table: "PendingSignups",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BillingStreet",
                table: "PendingSignups",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BillingVatId",
                table: "PendingSignups",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingCity",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "BillingCompanyName",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "BillingCountry",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "BillingEmail",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "BillingPostalCode",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "BillingReference",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "BillingStreet",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "BillingVatId",
                table: "PendingSignups");
        }
    }
}
