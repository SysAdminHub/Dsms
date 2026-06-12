using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class RenameLegalAcceptanceIpToAnonymized : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IpAddress",
                table: "LegalAcceptances",
                newName: "AnonymizedIpAddress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AnonymizedIpAddress",
                table: "LegalAcceptances",
                newName: "IpAddress");
        }
    }
}
