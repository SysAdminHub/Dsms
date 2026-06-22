using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceServiceProviderTypeEnumWithTenantEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Neue mandantenfähige Tabelle für Dienstleister-Arten anlegen.
            migrationBuilder.CreateTable(
                name: "ServiceProviderCategories",
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
                    table.PrimaryKey("PK_ServiceProviderCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProviderCategories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderCategories_TenantId",
                table: "ServiceProviderCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderCategories_TenantId_Name",
                table: "ServiceProviderCategories",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            // 2) Für jeden bestehenden Mandanten die Standard-Dienstleister-Arten anlegen.
            migrationBuilder.Sql("""
                INSERT INTO ServiceProviderCategories
                    (TenantId, Name, Description, SortOrder, IsActive, IsSystemDefault, CreatedAt)
                SELECT t.Id, d.Name, d.Description, d.SortOrder, 1, 1, UTC_TIMESTAMP()
                FROM Tenants t
                CROSS JOIN (
                    SELECT 'Hosting' AS Name, 'Hosting- und Rechenzentrumsleistungen' AS Description, 10 AS SortOrder
                    UNION ALL SELECT 'Cloud-Dienst', 'Cloud- und SaaS-Dienste', 20
                    UNION ALL SELECT 'IT-Support', 'IT-Betreuung und Support', 30
                    UNION ALL SELECT 'Softwareanbieter', 'Anbieter von Softwarelösungen', 40
                    UNION ALL SELECT 'Lohnabrechnung', 'Lohn- und Gehaltsabrechnung', 50
                    UNION ALL SELECT 'Buchhaltung', 'Buchhaltung und Finanzwesen', 60
                    UNION ALL SELECT 'Newsletter', 'Newsletter- und E-Mail-Versand', 70
                    UNION ALL SELECT 'CRM', 'Kundenbeziehungsmanagement', 80
                    UNION ALL SELECT 'Aktenvernichtung', 'Akten- und Datenträgervernichtung', 90
                    UNION ALL SELECT 'Wartung', 'Wartung und technischer Service', 100
                    UNION ALL SELECT 'Beratung', 'Beratungsleistungen', 110
                    UNION ALL SELECT 'Sonstige', 'Weitere Dienstleister-Arten', 120
                ) d
                WHERE NOT EXISTS (
                    SELECT 1 FROM ServiceProviderCategories spc
                    WHERE spc.TenantId = t.Id AND spc.Name = d.Name
                );
                """);

            // 3) Neue FK-Spalte anlegen (zunächst nullable für die Datenmigration).
            migrationBuilder.AddColumn<int>(
                name: "ServiceProviderCategoryId",
                table: "ServiceProviders",
                type: "int",
                nullable: true);

            // 4) Bestehende Dienstleister anhand des früheren Enum-Wertes der passenden Art zuordnen.
            migrationBuilder.Sql("""
                UPDATE ServiceProviders sp
                INNER JOIN ServiceProviderCategories spc
                    ON spc.TenantId = sp.TenantId
                    AND spc.Name = CASE sp.ProviderType
                        WHEN 0 THEN 'Hosting'
                        WHEN 1 THEN 'Cloud-Dienst'
                        WHEN 2 THEN 'IT-Support'
                        WHEN 3 THEN 'Softwareanbieter'
                        WHEN 4 THEN 'Lohnabrechnung'
                        WHEN 5 THEN 'Buchhaltung'
                        WHEN 6 THEN 'Newsletter'
                        WHEN 7 THEN 'CRM'
                        WHEN 8 THEN 'Aktenvernichtung'
                        WHEN 9 THEN 'Wartung'
                        WHEN 10 THEN 'Beratung'
                        WHEN 11 THEN 'Sonstige'
                        ELSE 'Sonstige'
                    END
                SET sp.ServiceProviderCategoryId = spc.Id;
                """);

            // 5) Altes Enum-Feld entfernen.
            migrationBuilder.DropColumn(
                name: "ProviderType",
                table: "ServiceProviders");

            // 6) Index und Fremdschlüssel auf die neue Art anlegen.
            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_ServiceProviderCategoryId",
                table: "ServiceProviders",
                column: "ServiceProviderCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceProviders_ServiceProviderCategories_ServiceProviderCa~",
                table: "ServiceProviders",
                column: "ServiceProviderCategoryId",
                principalTable: "ServiceProviderCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceProviders_ServiceProviderCategories_ServiceProviderCa~",
                table: "ServiceProviders");

            migrationBuilder.DropTable(
                name: "ServiceProviderCategories");

            migrationBuilder.DropIndex(
                name: "IX_ServiceProviders_ServiceProviderCategoryId",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "ServiceProviderCategoryId",
                table: "ServiceProviders");

            migrationBuilder.AddColumn<int>(
                name: "ProviderType",
                table: "ServiceProviders",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
