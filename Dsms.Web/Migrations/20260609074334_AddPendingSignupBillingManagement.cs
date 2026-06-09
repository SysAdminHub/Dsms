using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingSignupBillingManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingNote",
                table: "PendingSignups",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BillingStatus",
                table: "PendingSignups",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoicePaidAt",
                table: "PendingSignups",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceSentAt",
                table: "PendingSignups",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NextInvoiceDate",
                table: "PendingSignups",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingNote",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "BillingStatus",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "InvoicePaidAt",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "InvoiceSentAt",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "NextInvoiceDate",
                table: "PendingSignups");
        }
    }
}
