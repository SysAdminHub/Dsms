using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddArchivingSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Toms",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByUserId",
                table: "Toms",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Toms",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "ServiceProviders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByUserId",
                table: "ServiceProviders",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "ServiceProviders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "ProcessingActivities",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByUserId",
                table: "ProcessingActivities",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "ProcessingActivities",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Measures",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByUserId",
                table: "Measures",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Measures",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "EvidenceDocuments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByUserId",
                table: "EvidenceDocuments",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "EvidenceDocuments",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "DataProtectionImpactAssessments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByUserId",
                table: "DataProtectionImpactAssessments",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "DataProtectionImpactAssessments",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "AuditTemplates",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByUserId",
                table: "AuditTemplates",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "AuditTemplates",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "AuditRuns",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByUserId",
                table: "AuditRuns",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "AuditRuns",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Toms");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "Toms");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Toms");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "ProcessingActivities");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "ProcessingActivities");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "ProcessingActivities");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Measures");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "Measures");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Measures");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "EvidenceDocuments");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "EvidenceDocuments");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "EvidenceDocuments");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "DataProtectionImpactAssessments");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "DataProtectionImpactAssessments");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "DataProtectionImpactAssessments");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "AuditTemplates");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "AuditTemplates");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "AuditTemplates");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "AuditRuns");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "AuditRuns");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "AuditRuns");
        }
    }
}
