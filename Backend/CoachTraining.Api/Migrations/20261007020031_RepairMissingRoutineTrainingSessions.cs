using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class RepairMissingRoutineTrainingSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Earlier RoutineSchedule creation could commit the calendar row after
            // silently skipping TrainingSession generation on a coach conflict.
            // Repair only schedules that have never had any session at all; a
            // soft-deleted session is intentionally not recreated.
            migrationBuilder.Sql(
                """
                INSERT INTO "TrainingSessions" (
                    "TrainingType",
                    "RoutineScheduleId",
                    "SessionDate",
                    "ScheduledStartDateTime",
                    "ScheduledEndDateTime",
                    "AssignedCoachId",
                    "AssignedCoachCodeSnapshot",
                    "AssignedCoachNameSnapshot",
                    "Status",
                    "IsConflictOverridden",
                    "ConflictOverrideReason",
                    "CreatedDate",
                    "CreatedByUserId",
                    "IsDeleted")
                SELECT
                    'Routine',
                    rs."RoutineScheduleId",
                    rs."EffectiveStartDate",
                    rs."EffectiveStartDate" + rs."StartTime",
                    rs."EffectiveStartDate" + rs."EndTime",
                    rs."CoachId",
                    c."CoachCode",
                    c."FullName",
                    'Scheduled',
                    override_history."Reason" IS NOT NULL,
                    override_history."Reason",
                    rs."CreatedDate",
                    rs."CreatedByUserId",
                    FALSE
                FROM "RoutineSchedules" rs
                INNER JOIN "Coaches" c ON c."CoachId" = rs."CoachId"
                LEFT JOIN LATERAL (
                    SELECT coh."Reason"
                    FROM "ConflictOverrideHistories" coh
                    WHERE coh."RoutineScheduleId" = rs."RoutineScheduleId"
                    ORDER BY coh."ActionDate" DESC, coh."ConflictOverrideHistoryId" DESC
                    LIMIT 1
                ) override_history ON TRUE
                WHERE rs."IsDeleted" = FALSE
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "TrainingSessions" ts
                      WHERE ts."RoutineScheduleId" = rs."RoutineScheduleId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally preserve repaired operational records on rollback.
            // They may already contain attendance or approval history.
        }
    }
}
