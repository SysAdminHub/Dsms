using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceDocumentTypeAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DocumentCategory",
                table: "EvidenceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentType",
                table: "EvidenceDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentCategory",
                table: "EvidenceDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "EvidenceDocuments");
        }
    }
}
