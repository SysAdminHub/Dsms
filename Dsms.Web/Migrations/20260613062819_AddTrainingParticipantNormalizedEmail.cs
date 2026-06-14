using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingParticipantNormalizedEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @db = DATABASE();

                SET @sql = IF((SELECT COUNT(*) FROM information_schema.statistics
                    WHERE table_schema = @db AND table_name = 'TrainingParticipants'
                    AND index_name = 'IX_TrainingParticipants_Email') > 0,
                    'ALTER TABLE `TrainingParticipants` DROP INDEX `IX_TrainingParticipants_Email`', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @sql = IF((SELECT COUNT(*) FROM information_schema.statistics
                    WHERE table_schema = @db AND table_name = 'TrainingParticipants'
                    AND index_name = 'IX_TrainingParticipants_TenantId_Email') > 0,
                    'ALTER TABLE `TrainingParticipants` DROP INDEX `IX_TrainingParticipants_TenantId_Email`', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @sql = IF((SELECT COUNT(*) FROM information_schema.columns
                    WHERE table_schema = @db AND table_name = 'TrainingParticipants'
                    AND column_name = 'NormalizedEmail') = 0,
                    'ALTER TABLE `TrainingParticipants` ADD `NormalizedEmail` varchar(255) CHARACTER SET utf8mb4 NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                UPDATE `TrainingParticipants` SET `NormalizedEmail` = LOWER(TRIM(`Email`))
                WHERE `NormalizedEmail` IS NULL OR `NormalizedEmail` = '';

                ALTER TABLE `TrainingParticipants` MODIFY `NormalizedEmail` varchar(255) CHARACTER SET utf8mb4 NOT NULL;

                SET @sql = IF((SELECT COUNT(*) FROM information_schema.statistics
                    WHERE table_schema = @db AND table_name = 'TrainingParticipants'
                    AND index_name = 'IX_TrainingParticipants_NormalizedEmail') = 0,
                    'CREATE INDEX `IX_TrainingParticipants_NormalizedEmail` ON `TrainingParticipants` (`NormalizedEmail`)', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @sql = IF((SELECT COUNT(*) FROM information_schema.statistics
                    WHERE table_schema = @db AND table_name = 'TrainingParticipants'
                    AND index_name = 'IX_TrainingParticipants_TenantId_NormalizedEmail') = 0,
                    'CREATE UNIQUE INDEX `IX_TrainingParticipants_TenantId_NormalizedEmail` ON `TrainingParticipants` (`TenantId`, `NormalizedEmail`)', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingParticipants_NormalizedEmail",
                table: "TrainingParticipants");

            migrationBuilder.DropIndex(
                name: "IX_TrainingParticipants_TenantId_NormalizedEmail",
                table: "TrainingParticipants");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "TrainingParticipants");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingParticipants_Email",
                table: "TrainingParticipants",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingParticipants_TenantId_Email",
                table: "TrainingParticipants",
                columns: new[] { "TenantId", "Email" },
                unique: true);
        }
    }
}
