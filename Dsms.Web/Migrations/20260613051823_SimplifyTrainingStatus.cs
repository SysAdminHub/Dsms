using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyTrainingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Trainings SET Status = CASE Status
                    WHEN 0 THEN 91
                    WHEN 1 THEN 90
                    WHEN 2 THEN 2
                    WHEN 3 THEN 91
                    WHEN 4 THEN 91
                    WHEN 5 THEN 2
                    ELSE Status
                END;

                UPDATE Trainings SET Status = CASE Status
                    WHEN 90 THEN 0
                    WHEN 91 THEN 1
                    ELSE Status
                END;

                UPDATE Trainings SET ProofMissing = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Status-Rückmapping nicht unterstützt (alte Werte nicht eindeutig rekonstruierbar).
        }
    }
}
