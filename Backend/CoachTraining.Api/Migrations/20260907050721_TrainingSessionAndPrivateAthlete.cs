using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class TrainingSessionAndPrivateAthlete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingSessions",
                columns: table => new
                {
                    TrainingSessionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RoutineScheduleId = table.Column<int>(type: "integer", nullable: true),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScheduledStartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledEndDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualStartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualEndDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedCoachId = table.Column<int>(type: "integer", nullable: false),
                    AssignedCoachCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AssignedCoachNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActualCoachId = table.Column<int>(type: "integer", nullable: true),
                    ActualCoachCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ActualCoachNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    OriginalSessionId = table.Column<int>(type: "integer", nullable: true),
                    IsConflictOverridden = table.Column<bool>(type: "boolean", nullable: false),
                    ConflictOverrideReason = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSessions", x => x.TrainingSessionId);
                    table.ForeignKey(
                        name: "FK_TrainingSessions_Coaches_ActualCoachId",
                        column: x => x.ActualCoachId,
                        principalTable: "Coaches",
                        principalColumn: "CoachId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingSessions_Coaches_AssignedCoachId",
                        column: x => x.AssignedCoachId,
                        principalTable: "Coaches",
                        principalColumn: "CoachId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingSessions_RoutineSchedules_RoutineScheduleId",
                        column: x => x.RoutineScheduleId,
                        principalTable: "RoutineSchedules",
                        principalColumn: "RoutineScheduleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingSessions_TrainingSessions_OriginalSessionId",
                        column: x => x.OriginalSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "TrainingSessionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrivateSessionAthletes",
                columns: table => new
                {
                    PrivateSessionAthleteId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingSessionId = table.Column<int>(type: "integer", nullable: false),
                    AthleteId = table.Column<int>(type: "integer", nullable: false),
                    AthleteCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AthleteNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateSessionAthletes", x => x.PrivateSessionAthleteId);
                    table.ForeignKey(
                        name: "FK_PrivateSessionAthletes_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "AthleteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrivateSessionAthletes_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "TrainingSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrivateSessionAthletes_AthleteId",
                table: "PrivateSessionAthletes",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateSessionAthletes_TrainingSessionId_AthleteId",
                table: "PrivateSessionAthletes",
                columns: new[] { "TrainingSessionId", "AthleteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_ActualCoachId_SessionDate",
                table: "TrainingSessions",
                columns: new[] { "ActualCoachId", "SessionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_AssignedCoachId_SessionDate",
                table: "TrainingSessions",
                columns: new[] { "AssignedCoachId", "SessionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_OriginalSessionId",
                table: "TrainingSessions",
                column: "OriginalSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_RoutineScheduleId",
                table: "TrainingSessions",
                column: "RoutineScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_SessionDate",
                table: "TrainingSessions",
                column: "SessionDate");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_Status",
                table: "TrainingSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_TrainingType",
                table: "TrainingSessions",
                column: "TrainingType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivateSessionAthletes");

            migrationBuilder.DropTable(
                name: "TrainingSessions");
        }
    }
}
