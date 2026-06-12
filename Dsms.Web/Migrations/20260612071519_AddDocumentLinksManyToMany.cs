using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentLinksManyToMany : Migration
    {
        private static readonly string[] EvidenceDocumentForeignKeys =
        [
            "FK_EvidenceDocuments_AuditRuns_AuditRunId",
            "FK_EvidenceDocuments_DataProtectionImpactAssessments_DataProtec~",
            "FK_EvidenceDocuments_Measures_MeasureId",
            "FK_EvidenceDocuments_PrivacyIncidents_PrivacyIncidentId",
            "FK_EvidenceDocuments_ProcessingActivities_ProcessingActivityId",
            "FK_EvidenceDocuments_ServiceProviders_ServiceProviderId"
        ];

        private static readonly string[] EvidenceDocumentIndexes =
        [
            "IX_EvidenceDocuments_AuditRunId",
            "IX_EvidenceDocuments_DataProtectionImpactAssessmentId",
            "IX_EvidenceDocuments_MeasureId",
            "IX_EvidenceDocuments_PrivacyIncidentId",
            "IX_EvidenceDocuments_ProcessingActivityId",
            "IX_EvidenceDocuments_ServiceProviderId",
            "IX_EvidenceDocuments_TenantId_DataProtectionImpactAssessmentId",
            "IX_EvidenceDocuments_TenantId_PrivacyIncidentId",
            "IX_EvidenceDocuments_TenantId_ProcessingActivityId"
        ];

        private static readonly string[] EvidenceDocumentLinkColumns =
        [
            "AuditRunId",
            "DataProtectionImpactAssessmentId",
            "MeasureId",
            "PrivacyIncidentId",
            "ProcessingActivityId",
            "ServiceProviderId"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: FKs/Indizes/Spalten nur entfernen, wenn vorhanden (z. B. nach fehlgeschlagenem Erstlauf).
            foreach (var foreignKey in EvidenceDocumentForeignKeys)
            {
                migrationBuilder.Sql($"""
                    SET @schema_name = DATABASE();
                    SET @fk_exists = (
                        SELECT COUNT(*)
                        FROM information_schema.TABLE_CONSTRAINTS
                        WHERE TABLE_SCHEMA = @schema_name
                          AND TABLE_NAME = 'EvidenceDocuments'
                          AND CONSTRAINT_TYPE = 'FOREIGN KEY'
                          AND CONSTRAINT_NAME = '{foreignKey}'
                    );
                    SET @drop_fk_sql = IF(
                        @fk_exists > 0,
                        'ALTER TABLE `EvidenceDocuments` DROP FOREIGN KEY `{foreignKey}`',
                        'SELECT 1');
                    PREPARE drop_fk_stmt FROM @drop_fk_sql;
                    EXECUTE drop_fk_stmt;
                    DEALLOCATE PREPARE drop_fk_stmt;
                    """);
            }

            foreach (var index in EvidenceDocumentIndexes)
            {
                migrationBuilder.Sql($"""
                    SET @schema_name = DATABASE();
                    SET @index_exists = (
                        SELECT COUNT(*)
                        FROM information_schema.STATISTICS
                        WHERE TABLE_SCHEMA = @schema_name
                          AND TABLE_NAME = 'EvidenceDocuments'
                          AND INDEX_NAME = '{index}'
                    );
                    SET @drop_index_sql = IF(
                        @index_exists > 0,
                        'ALTER TABLE `EvidenceDocuments` DROP INDEX `{index}`',
                        'SELECT 1');
                    PREPARE drop_index_stmt FROM @drop_index_sql;
                    EXECUTE drop_index_stmt;
                    DEALLOCATE PREPARE drop_index_stmt;
                    """);
            }

            foreach (var column in EvidenceDocumentLinkColumns)
            {
                migrationBuilder.Sql($"""
                    SET @schema_name = DATABASE();
                    SET @column_exists = (
                        SELECT COUNT(*)
                        FROM information_schema.COLUMNS
                        WHERE TABLE_SCHEMA = @schema_name
                          AND TABLE_NAME = 'EvidenceDocuments'
                          AND COLUMN_NAME = '{column}'
                    );
                    SET @drop_column_sql = IF(
                        @column_exists > 0,
                        'ALTER TABLE `EvidenceDocuments` DROP COLUMN `{column}`',
                        'SELECT 1');
                    PREPARE drop_column_stmt FROM @drop_column_sql;
                    EXECUTE drop_column_stmt;
                    DEALLOCATE PREPARE drop_column_stmt;
                    """);
            }

            migrationBuilder.Sql("""
                SET @schema_name = DATABASE();
                SET @tenant_index_exists = (
                    SELECT COUNT(*)
                    FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = @schema_name
                      AND TABLE_NAME = 'EvidenceDocuments'
                      AND INDEX_NAME = 'IX_EvidenceDocuments_TenantId'
                );
                SET @create_tenant_index_sql = IF(
                    @tenant_index_exists = 0,
                    'CREATE INDEX `IX_EvidenceDocuments_TenantId` ON `EvidenceDocuments` (`TenantId`)',
                    'SELECT 1');
                PREPARE create_tenant_index_stmt FROM @create_tenant_index_sql;
                EXECUTE create_tenant_index_stmt;
                DEALLOCATE PREPARE create_tenant_index_stmt;
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS `DocumentLinks` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `TenantId` int NOT NULL,
                    `DocumentId` int NOT NULL,
                    `LinkedEntityType` int NOT NULL,
                    `LinkedEntityId` int NOT NULL,
                    `CreatedAt` datetime(6) NOT NULL,
                    `CreatedByUserId` varchar(450) CHARACTER SET utf8mb4 NULL,
                    CONSTRAINT `PK_DocumentLinks` PRIMARY KEY (`Id`),
                    CONSTRAINT `FK_DocumentLinks_EvidenceDocuments_DocumentId`
                        FOREIGN KEY (`DocumentId`) REFERENCES `EvidenceDocuments` (`Id`) ON DELETE CASCADE,
                    CONSTRAINT `FK_DocumentLinks_Tenants_TenantId`
                        FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
                ) CHARACTER SET=utf8mb4;
                """);

            migrationBuilder.Sql("""
                SET @schema_name = DATABASE();
                SET @index_exists = (
                    SELECT COUNT(*)
                    FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = @schema_name
                      AND TABLE_NAME = 'DocumentLinks'
                      AND INDEX_NAME = 'IX_DocumentLinks_DocumentId'
                );
                SET @create_index_sql = IF(
                    @index_exists = 0,
                    'CREATE INDEX `IX_DocumentLinks_DocumentId` ON `DocumentLinks` (`DocumentId`)',
                    'SELECT 1');
                PREPARE create_index_stmt FROM @create_index_sql;
                EXECUTE create_index_stmt;
                DEALLOCATE PREPARE create_index_stmt;
                """);

            migrationBuilder.Sql("""
                SET @schema_name = DATABASE();
                SET @index_exists = (
                    SELECT COUNT(*)
                    FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = @schema_name
                      AND TABLE_NAME = 'DocumentLinks'
                      AND INDEX_NAME = 'IX_DocumentLinks_LinkedEntityType_LinkedEntityId'
                );
                SET @create_index_sql = IF(
                    @index_exists = 0,
                    'CREATE INDEX `IX_DocumentLinks_LinkedEntityType_LinkedEntityId` ON `DocumentLinks` (`LinkedEntityType`, `LinkedEntityId`)',
                    'SELECT 1');
                PREPARE create_index_stmt FROM @create_index_sql;
                EXECUTE create_index_stmt;
                DEALLOCATE PREPARE create_index_stmt;
                """);

            migrationBuilder.Sql("""
                SET @schema_name = DATABASE();
                SET @index_exists = (
                    SELECT COUNT(*)
                    FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = @schema_name
                      AND TABLE_NAME = 'DocumentLinks'
                      AND INDEX_NAME = 'IX_DocumentLinks_TenantId'
                );
                SET @create_index_sql = IF(
                    @index_exists = 0,
                    'CREATE INDEX `IX_DocumentLinks_TenantId` ON `DocumentLinks` (`TenantId`)',
                    'SELECT 1');
                PREPARE create_index_stmt FROM @create_index_sql;
                EXECUTE create_index_stmt;
                DEALLOCATE PREPARE create_index_stmt;
                """);

            migrationBuilder.Sql("""
                SET @schema_name = DATABASE();
                SET @index_exists = (
                    SELECT COUNT(*)
                    FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = @schema_name
                      AND TABLE_NAME = 'DocumentLinks'
                      AND INDEX_NAME = 'IX_DocumentLinks_TenantId_DocumentId_LinkedEntityType_LinkedEnt~'
                );
                SET @create_index_sql = IF(
                    @index_exists = 0,
                    'CREATE UNIQUE INDEX `IX_DocumentLinks_TenantId_DocumentId_LinkedEntityType_LinkedEnt~` ON `DocumentLinks` (`TenantId`, `DocumentId`, `LinkedEntityType`, `LinkedEntityId`)',
                    'SELECT 1');
                PREPARE create_index_stmt FROM @create_index_sql;
                EXECUTE create_index_stmt;
                DEALLOCATE PREPARE create_index_stmt;
                """);

            migrationBuilder.Sql("""
                SET @schema_name = DATABASE();
                SET @index_exists = (
                    SELECT COUNT(*)
                    FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = @schema_name
                      AND TABLE_NAME = 'DocumentLinks'
                      AND INDEX_NAME = 'IX_DocumentLinks_TenantId_LinkedEntityType_LinkedEntityId'
                );
                SET @create_index_sql = IF(
                    @index_exists = 0,
                    'CREATE INDEX `IX_DocumentLinks_TenantId_LinkedEntityType_LinkedEntityId` ON `DocumentLinks` (`TenantId`, `LinkedEntityType`, `LinkedEntityId`)',
                    'SELECT 1');
                PREPARE create_index_stmt FROM @create_index_sql;
                EXECUTE create_index_stmt;
                DEALLOCATE PREPARE create_index_stmt;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentLinks");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceDocuments_TenantId",
                table: "EvidenceDocuments");

            migrationBuilder.AddColumn<int>(
                name: "AuditRunId",
                table: "EvidenceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DataProtectionImpactAssessmentId",
                table: "EvidenceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MeasureId",
                table: "EvidenceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrivacyIncidentId",
                table: "EvidenceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingActivityId",
                table: "EvidenceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceProviderId",
                table: "EvidenceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_AuditRunId",
                table: "EvidenceDocuments",
                column: "AuditRunId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_DataProtectionImpactAssessmentId",
                table: "EvidenceDocuments",
                column: "DataProtectionImpactAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_MeasureId",
                table: "EvidenceDocuments",
                column: "MeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_PrivacyIncidentId",
                table: "EvidenceDocuments",
                column: "PrivacyIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_ProcessingActivityId",
                table: "EvidenceDocuments",
                column: "ProcessingActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_ServiceProviderId",
                table: "EvidenceDocuments",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_TenantId_DataProtectionImpactAssessmentId",
                table: "EvidenceDocuments",
                columns: new[] { "TenantId", "DataProtectionImpactAssessmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_TenantId_PrivacyIncidentId",
                table: "EvidenceDocuments",
                columns: new[] { "TenantId", "PrivacyIncidentId" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_TenantId_ProcessingActivityId",
                table: "EvidenceDocuments",
                columns: new[] { "TenantId", "ProcessingActivityId" });

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceDocuments_AuditRuns_AuditRunId",
                table: "EvidenceDocuments",
                column: "AuditRunId",
                principalTable: "AuditRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceDocuments_DataProtectionImpactAssessments_DataProtec~",
                table: "EvidenceDocuments",
                column: "DataProtectionImpactAssessmentId",
                principalTable: "DataProtectionImpactAssessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceDocuments_Measures_MeasureId",
                table: "EvidenceDocuments",
                column: "MeasureId",
                principalTable: "Measures",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceDocuments_PrivacyIncidents_PrivacyIncidentId",
                table: "EvidenceDocuments",
                column: "PrivacyIncidentId",
                principalTable: "PrivacyIncidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceDocuments_ProcessingActivities_ProcessingActivityId",
                table: "EvidenceDocuments",
                column: "ProcessingActivityId",
                principalTable: "ProcessingActivities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceDocuments_ServiceProviders_ServiceProviderId",
                table: "EvidenceDocuments",
                column: "ServiceProviderId",
                principalTable: "ServiceProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
