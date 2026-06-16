using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingAssignmentCertificateDocumentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingAssignments_TenantId",
                table: "TrainingAssignments");

            migrationBuilder.AddColumn<int>(
                name: "CertificateDocumentId",
                table: "TrainingAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_CertificateDocumentId",
                table: "TrainingAssignments",
                column: "CertificateDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingAssignments_EvidenceDocuments_CertificateDocumentId",
                table: "TrainingAssignments",
                column: "CertificateDocumentId",
                principalTable: "EvidenceDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingAssignments_EvidenceDocuments_CertificateDocumentId",
                table: "TrainingAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TrainingAssignments_CertificateDocumentId",
                table: "TrainingAssignments");

            migrationBuilder.DropColumn(
                name: "CertificateDocumentId",
                table: "TrainingAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_TenantId",
                table: "TrainingAssignments",
                column: "TenantId");
        }
    }
}
