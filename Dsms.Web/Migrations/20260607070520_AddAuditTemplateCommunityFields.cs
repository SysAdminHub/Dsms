using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditTemplateCommunityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommunityStatus",
                table: "AuditTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OriginalTemplateId",
                table: "AuditTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalTenantId",
                table: "AuditTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewComment",
                table: "AuditTemplates",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "AuditTemplates",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByUserId",
                table: "AuditTemplates",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "AuditTemplates",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedByUserId",
                table: "AuditTemplates",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommunityStatus",
                table: "AuditTemplates");

            migrationBuilder.DropColumn(
                name: "OriginalTemplateId",
                table: "AuditTemplates");

            migrationBuilder.DropColumn(
                name: "OriginalTenantId",
                table: "AuditTemplates");

            migrationBuilder.DropColumn(
                name: "ReviewComment",
                table: "AuditTemplates");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "AuditTemplates");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "AuditTemplates");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "AuditTemplates");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "AuditTemplates");
        }
    }
}
