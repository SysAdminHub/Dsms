using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingSignupDiscountFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "PendingSignups",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DiscountCodeId",
                table: "PendingSignups",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "DiscountCodeSnapshot",
                table: "PendingSignups",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DiscountFreeMonthsSnapshot",
                table: "PendingSignups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountNameSnapshot",
                table: "PendingSignups",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DiscountTypeSnapshot",
                table: "PendingSignups",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValueSnapshot",
                table: "PendingSignups",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalAmount",
                table: "PendingSignups",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAmount",
                table: "PendingSignups",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "DiscountCodeId",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "DiscountCodeSnapshot",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "DiscountFreeMonthsSnapshot",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "DiscountNameSnapshot",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "DiscountTypeSnapshot",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "DiscountValueSnapshot",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "FinalAmount",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "PendingSignups");
        }
    }
}
