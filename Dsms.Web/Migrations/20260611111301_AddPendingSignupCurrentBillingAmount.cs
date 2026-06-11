using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingSignupCurrentBillingAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentBillingAmount",
                table: "PendingSignups",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentBillingAmountUpdatedAt",
                table: "PendingSignups",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentBillingAmountUpdatedByUserId",
                table: "PendingSignups",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CurrentBillingCurrency",
                table: "PendingSignups",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CurrentBillingCycle",
                table: "PendingSignups",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentBillingAmount",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "CurrentBillingAmountUpdatedAt",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "CurrentBillingAmountUpdatedByUserId",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "CurrentBillingCurrency",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "CurrentBillingCycle",
                table: "PendingSignups");
        }
    }
}
