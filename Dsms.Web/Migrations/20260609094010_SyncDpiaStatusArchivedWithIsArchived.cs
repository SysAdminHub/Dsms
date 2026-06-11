using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class SyncDpiaStatusArchivedWithIsArchived : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DpiaStatus.Archived = 5 – ältere Einträge nur mit Status, ohne IsArchived
            migrationBuilder.Sql("""
                UPDATE DataProtectionImpactAssessments
                SET IsArchived = 1,
                    ArchivedAt = COALESCE(ArchivedAt, UpdatedAt, CreatedAt)
                WHERE Status = 5 AND (IsArchived = 0 OR IsArchived IS NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
