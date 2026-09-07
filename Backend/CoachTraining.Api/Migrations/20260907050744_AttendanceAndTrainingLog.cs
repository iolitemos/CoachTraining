using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceAndTrainingLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    AttendanceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingSessionId = table.Column<int>(type: "integer", nullable: false),
                    AthleteId = table.Column<int>(type: "integer", nullable: false),
                    AthleteCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AthleteNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ArrivalTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Remark = table.Column<string>(type: "text", nullable: true),
                    RecordedByUserId = table.Column<int>(type: "integer", nullable: false),
                    RecordedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.AttendanceId);
                    table.ForeignKey(
                        name: "FK_Attendances_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "AthleteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "TrainingSessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attendances_Users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingLogs",
                columns: table => new
                {
                    TrainingLogId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingSessionId = table.Column<int>(type: "integer", nullable: false),
                    Topic = table.Column<string>(type: "text", nullable: true),
                    Objective = table.Column<string>(type: "text", nullable: true),
                    ExerciseDrill = table.Column<string>(type: "text", nullable: true),
                    Focus = table.Column<string>(type: "text", nullable: true),
                    Intensity = table.Column<string>(type: "text", nullable: true),
                    CoachNotes = table.Column<string>(type: "text", nullable: true),
                    AthleteNotes = table.Column<string>(type: "text", nullable: true),
                    GeneralRemarks = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingLogs", x => x.TrainingLogId);
                    table.ForeignKey(
                        name: "FK_TrainingLogs_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "TrainingSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_AthleteId",
                table: "Attendances",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_RecordedByUserId",
                table: "Attendances",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_TrainingSessionId_AthleteId",
                table: "Attendances",
                columns: new[] { "TrainingSessionId", "AthleteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLogs_TrainingSessionId",
                table: "TrainingLogs",
                column: "TrainingSessionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "TrainingLogs");
        }
    }
}
