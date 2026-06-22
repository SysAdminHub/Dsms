using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTomCategoryEnumWithTenantEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Neue mandantenfähige Kategorie-Tabelle anlegen.
            migrationBuilder.CreateTable(
                name: "TomCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
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
                    table.PrimaryKey("PK_TomCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TomCategories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TomCategories_TenantId",
                table: "TomCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TomCategories_TenantId_Name",
                table: "TomCategories",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            // 2) Für jeden bestehenden Mandanten die Standard-TOM-Kategorien anlegen.
            migrationBuilder.Sql("""
                INSERT INTO TomCategories
                    (TenantId, Name, Description, SortOrder, IsActive, IsSystemDefault, CreatedAt)
                SELECT t.Id, d.Name, d.Description, d.SortOrder, 1, 1, UTC_TIMESTAMP()
                FROM Tenants t
                CROSS JOIN (
                    SELECT 'Zutrittskontrolle' AS Name, 'Schutz vor unbefugtem Zutritt zu Räumen und Anlagen' AS Description, 10 AS SortOrder
                    UNION ALL SELECT 'Zugangskontrolle', 'Schutz vor unbefugter Systemnutzung', 20
                    UNION ALL SELECT 'Zugriffskontrolle', 'Beschränkung der Datenzugriffe auf Berechtigte', 30
                    UNION ALL SELECT 'Weitergabekontrolle', 'Schutz bei Transport und Übermittlung von Daten', 40
                    UNION ALL SELECT 'Eingabekontrolle', 'Nachvollziehbarkeit von Eingabe, Änderung und Löschung', 50
                    UNION ALL SELECT 'Auftragskontrolle', 'Weisungsgemäße Verarbeitung durch Auftragsverarbeiter', 60
                    UNION ALL SELECT 'Verfügbarkeitskontrolle', 'Schutz vor Verlust und Sicherstellung der Verfügbarkeit', 70
                    UNION ALL SELECT 'Trennungsgebot', 'Getrennte Verarbeitung zu unterschiedlichen Zwecken', 80
                    UNION ALL SELECT 'Verschlüsselung', 'Schutz von Daten durch Verschlüsselungsverfahren', 90
                    UNION ALL SELECT 'Backup und Wiederherstellung', 'Datensicherung und Wiederherstellbarkeit', 100
                    UNION ALL SELECT 'Protokollierung', 'Protokollierung sicherheitsrelevanter Ereignisse', 110
                    UNION ALL SELECT 'Berechtigungskonzept', 'Rollen- und Rechteverwaltung', 120
                    UNION ALL SELECT 'Schulung und Sensibilisierung', 'Awareness und Schulung der Beschäftigten', 130
                    UNION ALL SELECT 'Sonstige', 'Weitere technische und organisatorische Maßnahmen', 140
                ) d
                WHERE NOT EXISTS (
                    SELECT 1 FROM TomCategories tc
                    WHERE tc.TenantId = t.Id AND tc.Name = d.Name
                );
                """);

            // 3) Neue FK-Spalte anlegen (zunächst nullable für die Datenmigration).
            migrationBuilder.AddColumn<int>(
                name: "TomCategoryId",
                table: "Toms",
                type: "int",
                nullable: true);

            // 4) Bestehende TOMs anhand des früheren Enum-Wertes der passenden Kategorie zuordnen.
            migrationBuilder.Sql("""
                UPDATE Toms tm
                INNER JOIN TomCategories tc
                    ON tc.TenantId = tm.TenantId
                    AND tc.Name = CASE tm.Category
                        WHEN 0 THEN 'Zutrittskontrolle'
                        WHEN 1 THEN 'Zugangskontrolle'
                        WHEN 2 THEN 'Zugriffskontrolle'
                        WHEN 3 THEN 'Weitergabekontrolle'
                        WHEN 4 THEN 'Eingabekontrolle'
                        WHEN 5 THEN 'Auftragskontrolle'
                        WHEN 6 THEN 'Verfügbarkeitskontrolle'
                        WHEN 7 THEN 'Trennungsgebot'
                        WHEN 8 THEN 'Verschlüsselung'
                        WHEN 9 THEN 'Backup und Wiederherstellung'
                        WHEN 10 THEN 'Protokollierung'
                        WHEN 11 THEN 'Berechtigungskonzept'
                        WHEN 12 THEN 'Schulung und Sensibilisierung'
                        WHEN 13 THEN 'Sonstige'
                        ELSE 'Sonstige'
                    END
                SET tm.TomCategoryId = tc.Id;
                """);

            // 5) Altes Enum-Feld entfernen.
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Toms");

            // 6) Index und Fremdschlüssel auf die neue Kategorie anlegen.
            migrationBuilder.CreateIndex(
                name: "IX_Toms_TomCategoryId",
                table: "Toms",
                column: "TomCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Toms_TomCategories_TomCategoryId",
                table: "Toms",
                column: "TomCategoryId",
                principalTable: "TomCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Toms_TomCategories_TomCategoryId",
                table: "Toms");

            migrationBuilder.DropTable(
                name: "TomCategories");

            migrationBuilder.DropIndex(
                name: "IX_Toms_TomCategoryId",
                table: "Toms");

            migrationBuilder.DropColumn(
                name: "TomCategoryId",
                table: "Toms");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Toms",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
