using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantDeletionRequestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionRequestedAt",
                table: "Tenants",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionRequestedByUserId",
                table: "Tenants",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionScheduledAt",
                table: "Tenants",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeletionRequested",
                table: "Tenants",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE Tenants SET IsDeletionRequested = 0 WHERE IsDeletionRequested IS NULL;
                UPDATE Tenants SET IsActive = 1 WHERE IsActive IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletionRequestedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DeletionRequestedByUserId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DeletionScheduledAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsDeletionRequested",
                table: "Tenants");
        }
    }
}
