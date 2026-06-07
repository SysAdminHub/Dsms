using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditTemplateTypeAndSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TenantId",
                table: "AuditTemplates",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "TemplateType",
                table: "AuditTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TemplateTitleSnapshot",
                table: "AuditRuns",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TemplateVersionSnapshot",
                table: "AuditRuns",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "QuestionCategory",
                table: "AuditAnswers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "QuestionIsRequired",
                table: "AuditAnswers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "QuestionSortOrder",
                table: "AuditAnswers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "QuestionText",
                table: "AuditAnswers",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Bestehende Mandantenvorlagen als Tenant markieren (Default 0); ohne TenantId → Official.
            migrationBuilder.Sql("""
                UPDATE AuditTemplates
                SET TemplateType = 1
                WHERE TenantId IS NULL;
                """);

            // Frage-Snapshots für bestehende Audit-Antworten aus Vorlagenfragen übernehmen.
            migrationBuilder.Sql("""
                UPDATE AuditAnswers aa
                INNER JOIN AuditQuestions aq ON aa.AuditQuestionId = aq.Id
                SET aa.QuestionText = aq.Text,
                    aa.QuestionSortOrder = aq.SortOrder,
                    aa.QuestionCategory = aq.Category,
                    aa.QuestionIsRequired = aq.IsRequired;
                """);

            // Vorlagen-Snapshots für bestehende Audit-Durchläufe übernehmen.
            migrationBuilder.Sql("""
                UPDATE AuditRuns ar
                INNER JOIN AuditTemplates at ON ar.AuditTemplateId = at.Id
                SET ar.TemplateTitleSnapshot = at.Title,
                    ar.TemplateVersionSnapshot = at.Version;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemplateType",
                table: "AuditTemplates");

            migrationBuilder.DropColumn(
                name: "TemplateTitleSnapshot",
                table: "AuditRuns");

            migrationBuilder.DropColumn(
                name: "TemplateVersionSnapshot",
                table: "AuditRuns");

            migrationBuilder.DropColumn(
                name: "QuestionCategory",
                table: "AuditAnswers");

            migrationBuilder.DropColumn(
                name: "QuestionIsRequired",
                table: "AuditAnswers");

            migrationBuilder.DropColumn(
                name: "QuestionSortOrder",
                table: "AuditAnswers");

            migrationBuilder.DropColumn(
                name: "QuestionText",
                table: "AuditAnswers");

            migrationBuilder.AlterColumn<int>(
                name: "TenantId",
                table: "AuditTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
