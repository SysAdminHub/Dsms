using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceDocumentCategoryEnumWithTenantEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSystemDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentCategories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentCategories_TenantId",
                table: "DocumentCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentCategories_TenantId_Name",
                table: "DocumentCategories",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO DocumentCategories
                    (TenantId, Name, Description, Color, SortOrder, IsActive, IsSystemDefault, CreatedAt)
                SELECT t.Id, d.Name, d.Description, d.Color, d.SortOrder, 1, 1, UTC_TIMESTAMP()
                FROM Tenants t
                CROSS JOIN (
                    SELECT 'Datenschutz' AS Name, 'Datenschutzbezogene Dokumente' AS Description, 'blue' AS Color, 10 AS SortOrder
                    UNION ALL SELECT 'IT-Sicherheit', 'IT- und Informationssicherheit', 'cyan', 20
                    UNION ALL SELECT 'HR', 'Personal- und HR-bezogene Dokumente', 'purple', 30
                    UNION ALL SELECT 'Lieferanten', 'Dienstleister- und Lieferantenunterlagen', 'orange', 40
                    UNION ALL SELECT 'Betroffenenrechte', 'Anfragen und Rechte betroffener Personen', 'green', 50
                    UNION ALL SELECT 'Datenschutzvorfälle', 'Vorfälle und Meldungen', 'red', 60
                    UNION ALL SELECT 'Informationspflichten', 'Informations- und Transparenzpflichten', 'blue', 70
                    UNION ALL SELECT 'Allgemein', 'Allgemeine Dokumente', 'gray', 80
                ) d
                WHERE NOT EXISTS (
                    SELECT 1 FROM DocumentCategories dc
                    WHERE dc.TenantId = t.Id AND dc.Name = d.Name
                );
                """);

            migrationBuilder.AddColumn<int>(
                name: "DocumentCategoryId",
                table: "EvidenceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE EvidenceDocuments ed
                INNER JOIN DocumentCategories dc
                    ON dc.TenantId = ed.TenantId
                    AND dc.Name = CASE ed.DocumentCategory
                        WHEN 0 THEN 'Datenschutz'
                        WHEN 1 THEN 'IT-Sicherheit'
                        WHEN 2 THEN 'HR'
                        WHEN 3 THEN 'Lieferanten'
                        WHEN 4 THEN 'Betroffenenrechte'
                        WHEN 5 THEN 'Datenschutzvorfälle'
                        WHEN 6 THEN 'Informationspflichten'
                        WHEN 7 THEN 'Allgemein'
                        ELSE NULL
                    END
                SET ed.DocumentCategoryId = dc.Id
                WHERE ed.DocumentCategory IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "DocumentCategory",
                table: "EvidenceDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_DocumentCategoryId",
                table: "EvidenceDocuments",
                column: "DocumentCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceDocuments_DocumentCategories_DocumentCategoryId",
                table: "EvidenceDocuments",
                column: "DocumentCategoryId",
                principalTable: "DocumentCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceDocuments_DocumentCategories_DocumentCategoryId",
                table: "EvidenceDocuments");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceDocuments_DocumentCategoryId",
                table: "EvidenceDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentCategoryId",
                table: "EvidenceDocuments");

            migrationBuilder.AddColumn<int>(
                name: "DocumentCategory",
                table: "EvidenceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.DropTable(
                name: "DocumentCategories");
        }
    }
}
