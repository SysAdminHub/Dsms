using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsms.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingParticipantsAndAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Department = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalReference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsArchived = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ArchivedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingParticipants_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TrainingId = table.Column<int>(type: "int", nullable: false),
                    TrainingParticipantId = table.Column<int>(type: "int", nullable: true),
                    ParticipantNameSnapshot = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParticipantEmailSnapshot = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccessCodeHash = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccessCodeGeneratedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AccessCodeExpiresAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AccessCodeSentAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    InvitationSentAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    InvitationSentByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InvitationSendCount = table.Column<int>(type: "int", nullable: false),
                    LastAccessAttemptAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FailedAccessAttempts = table.Column<int>(type: "int", nullable: false),
                    LockedUntilUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ParticipationConfirmedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsArchived = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ArchivedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingAssignments_AspNetUsers_InvitationSentByUserId",
                        column: x => x.InvitationSentByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrainingAssignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingAssignments_TrainingParticipants_TrainingParticipant~",
                        column: x => x.TrainingParticipantId,
                        principalTable: "TrainingParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrainingAssignments_Trainings_TrainingId",
                        column: x => x.TrainingId,
                        principalTable: "Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_AccessCodeExpiresAtUtc",
                table: "TrainingAssignments",
                column: "AccessCodeExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_InvitationSentByUserId",
                table: "TrainingAssignments",
                column: "InvitationSentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_LockedUntilUtc",
                table: "TrainingAssignments",
                column: "LockedUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_ParticipantEmailSnapshot",
                table: "TrainingAssignments",
                column: "ParticipantEmailSnapshot");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_Status",
                table: "TrainingAssignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_TenantId",
                table: "TrainingAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_TenantId_TrainingId",
                table: "TrainingAssignments",
                columns: new[] { "TenantId", "TrainingId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_TrainingId",
                table: "TrainingAssignments",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_TrainingParticipantId",
                table: "TrainingAssignments",
                column: "TrainingParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingParticipants_Email",
                table: "TrainingParticipants",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingParticipants_TenantId",
                table: "TrainingParticipants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingParticipants_TenantId_Email",
                table: "TrainingParticipants",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingParticipants_TenantId_IsActive",
                table: "TrainingParticipants",
                columns: new[] { "TenantId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingAssignments");

            migrationBuilder.DropTable(
                name: "TrainingParticipants");
        }
    }
}
