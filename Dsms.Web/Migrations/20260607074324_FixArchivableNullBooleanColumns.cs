using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixArchivableNullBooleanColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Toms SET IsArchived = 0 WHERE IsArchived IS NULL;
                UPDATE ServiceProviders SET IsArchived = 0 WHERE IsArchived IS NULL;
                UPDATE ProcessingActivities SET IsArchived = 0 WHERE IsArchived IS NULL;
                UPDATE Measures SET IsArchived = 0 WHERE IsArchived IS NULL;
                UPDATE EvidenceDocuments SET IsArchived = 0 WHERE IsArchived IS NULL;
                UPDATE DataProtectionImpactAssessments SET IsArchived = 0 WHERE IsArchived IS NULL;
                UPDATE AuditTemplates SET IsArchived = 0 WHERE IsArchived IS NULL;
                UPDATE AuditTemplates SET IsActive = 1 WHERE IsActive IS NULL;
                UPDATE AuditRuns SET IsArchived = 0 WHERE IsArchived IS NULL;
                UPDATE AspNetUsers SET IsActive = 1 WHERE IsActive IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
