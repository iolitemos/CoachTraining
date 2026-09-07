using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class SubstitutionApprovalConflictAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreviousValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    ActionByUserId = table.Column<int>(type: "integer", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_ActionByUserId",
                        column: x => x.ActionByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoachSubstitutionHistories",
                columns: table => new
                {
                    CoachSubstitutionHistoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingSessionId = table.Column<int>(type: "integer", nullable: false),
                    OriginalCoachId = table.Column<int>(type: "integer", nullable: false),
                    SubstituteCoachId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    ActionByUserId = table.Column<int>(type: "integer", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachSubstitutionHistories", x => x.CoachSubstitutionHistoryId);
                    table.ForeignKey(
                        name: "FK_CoachSubstitutionHistories_Coaches_OriginalCoachId",
                        column: x => x.OriginalCoachId,
                        principalTable: "Coaches",
                        principalColumn: "CoachId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachSubstitutionHistories_Coaches_SubstituteCoachId",
                        column: x => x.SubstituteCoachId,
                        principalTable: "Coaches",
                        principalColumn: "CoachId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachSubstitutionHistories_TrainingSessions_TrainingSession~",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "TrainingSessionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachSubstitutionHistories_Users_ActionByUserId",
                        column: x => x.ActionByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConflictOverrideHistories",
                columns: table => new
                {
                    ConflictOverrideHistoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConflictType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TrainingSessionId = table.Column<int>(type: "integer", nullable: true),
                    RoutineScheduleId = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    ActionByUserId = table.Column<int>(type: "integer", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConflictOverrideHistories", x => x.ConflictOverrideHistoryId);
                    table.ForeignKey(
                        name: "FK_ConflictOverrideHistories_RoutineSchedules_RoutineScheduleId",
                        column: x => x.RoutineScheduleId,
                        principalTable: "RoutineSchedules",
                        principalColumn: "RoutineScheduleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConflictOverrideHistories_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "TrainingSessionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConflictOverrideHistories_Users_ActionByUserId",
                        column: x => x.ActionByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingApprovalHistories",
                columns: table => new
                {
                    TrainingApprovalHistoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingSessionId = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    ActionByUserId = table.Column<int>(type: "integer", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingApprovalHistories", x => x.TrainingApprovalHistoryId);
                    table.ForeignKey(
                        name: "FK_TrainingApprovalHistories_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "TrainingSessionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingApprovalHistories_Users_ActionByUserId",
                        column: x => x.ActionByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActionByUserId",
                table: "AuditLogs",
                column: "ActionByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_CoachSubstitutionHistories_ActionByUserId",
                table: "CoachSubstitutionHistories",
                column: "ActionByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachSubstitutionHistories_OriginalCoachId",
                table: "CoachSubstitutionHistories",
                column: "OriginalCoachId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachSubstitutionHistories_SubstituteCoachId",
                table: "CoachSubstitutionHistories",
                column: "SubstituteCoachId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachSubstitutionHistories_TrainingSessionId",
                table: "CoachSubstitutionHistories",
                column: "TrainingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConflictOverrideHistories_ActionByUserId",
                table: "ConflictOverrideHistories",
                column: "ActionByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConflictOverrideHistories_RoutineScheduleId",
                table: "ConflictOverrideHistories",
                column: "RoutineScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ConflictOverrideHistories_TrainingSessionId",
                table: "ConflictOverrideHistories",
                column: "TrainingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingApprovalHistories_ActionByUserId",
                table: "TrainingApprovalHistories",
                column: "ActionByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingApprovalHistories_TrainingSessionId",
                table: "TrainingApprovalHistories",
                column: "TrainingSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CoachSubstitutionHistories");

            migrationBuilder.DropTable(
                name: "ConflictOverrideHistories");

            migrationBuilder.DropTable(
                name: "TrainingApprovalHistories");
        }
    }
}
