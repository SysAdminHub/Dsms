using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMeasureAuditAnswerLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuditAnswerId",
                table: "Measures",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Measures_AuditAnswerId",
                table: "Measures",
                column: "AuditAnswerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Measures_AuditAnswers_AuditAnswerId",
                table: "Measures",
                column: "AuditAnswerId",
                principalTable: "AuditAnswers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Measures_AuditAnswers_AuditAnswerId",
                table: "Measures");

            migrationBuilder.DropIndex(
                name: "IX_Measures_AuditAnswerId",
                table: "Measures");

            migrationBuilder.DropColumn(
                name: "AuditAnswerId",
                table: "Measures");
        }
    }
}
