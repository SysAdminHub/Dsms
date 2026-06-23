using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingActivityLegalBases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessingActivityLegalBases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ProcessingActivityId = table.Column<int>(type: "int", nullable: false),
                    LegalBasisKey = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingActivityLegalBases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingActivityLegalBases_ProcessingActivities_Processing~",
                        column: x => x.ProcessingActivityId,
                        principalTable: "ProcessingActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcessingActivityLegalBases_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingActivityLegalBases_ProcessingActivityId_LegalBasis~",
                table: "ProcessingActivityLegalBases",
                columns: new[] { "ProcessingActivityId", "LegalBasisKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingActivityLegalBases_TenantId",
                table: "ProcessingActivityLegalBases",
                column: "TenantId");

            // Konservative, ausschließlich eindeutige Vorauswahl bestehender Freitext-Rechtsgrundlagen:
            // Nur wenn der getrimmte Freitext EXAKT einem bekannten Gesetzesartikel entspricht, wird der
            // passende Key vorausgewählt. Kombinationen oder freie Beschreibungen bleiben unangetastet.
            // Der bestehende Freitext wird NICHT verändert und bleibt vollständig erhalten.
            migrationBuilder.Sql("""
                INSERT INTO ProcessingActivityLegalBases (TenantId, ProcessingActivityId, LegalBasisKey, CreatedAt)
                SELECT p.TenantId, p.Id, m.LegalBasisKey, UTC_TIMESTAMP()
                FROM ProcessingActivities p
                INNER JOIN (
                    SELECT 'Art. 6 Abs. 1 lit. a DSGVO' AS Article, 'Art6_1_a' AS LegalBasisKey
                    UNION ALL SELECT 'Art. 6 Abs. 1 lit. b DSGVO', 'Art6_1_b'
                    UNION ALL SELECT 'Art. 6 Abs. 1 lit. c DSGVO', 'Art6_1_c'
                    UNION ALL SELECT 'Art. 6 Abs. 1 lit. d DSGVO', 'Art6_1_d'
                    UNION ALL SELECT 'Art. 6 Abs. 1 lit. e DSGVO', 'Art6_1_e'
                    UNION ALL SELECT 'Art. 6 Abs. 1 lit. f DSGVO', 'Art6_1_f'
                    UNION ALL SELECT 'Art. 10 DSGVO', 'Art10'
                ) m ON TRIM(p.LegalBasis) = m.Article
                WHERE p.LegalBasis IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM ProcessingActivityLegalBases x
                      WHERE x.ProcessingActivityId = p.Id AND x.LegalBasisKey = m.LegalBasisKey
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessingActivityLegalBases");
        }
    }
}
