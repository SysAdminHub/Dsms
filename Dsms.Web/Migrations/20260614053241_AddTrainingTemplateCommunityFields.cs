using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingTemplateCommunityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommunityRejectionReason",
                table: "TrainingTemplates",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CommunitySubmissionNote",
                table: "TrainingTemplates",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CommunitySubmittedByTenantId",
                table: "TrainingTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceTemplateId",
                table: "TrainingTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplates_CommunitySubmittedByTenantId",
                table: "TrainingTemplates",
                column: "CommunitySubmittedByTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTemplates_SourceTemplateId",
                table: "TrainingTemplates",
                column: "SourceTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTemplates_Tenants_CommunitySubmittedByTenantId",
                table: "TrainingTemplates",
                column: "CommunitySubmittedByTenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTemplates_TrainingTemplates_SourceTemplateId",
                table: "TrainingTemplates",
                column: "SourceTemplateId",
                principalTable: "TrainingTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTemplates_Tenants_CommunitySubmittedByTenantId",
                table: "TrainingTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTemplates_TrainingTemplates_SourceTemplateId",
                table: "TrainingTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TrainingTemplates_CommunitySubmittedByTenantId",
                table: "TrainingTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TrainingTemplates_SourceTemplateId",
                table: "TrainingTemplates");

            migrationBuilder.DropColumn(
                name: "CommunityRejectionReason",
                table: "TrainingTemplates");

            migrationBuilder.DropColumn(
                name: "CommunitySubmissionNote",
                table: "TrainingTemplates");

            migrationBuilder.DropColumn(
                name: "CommunitySubmittedByTenantId",
                table: "TrainingTemplates");

            migrationBuilder.DropColumn(
                name: "SourceTemplateId",
                table: "TrainingTemplates");
        }
    }
}
