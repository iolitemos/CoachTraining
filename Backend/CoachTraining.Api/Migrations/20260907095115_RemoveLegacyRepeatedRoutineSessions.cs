using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyRepeatedRoutineSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The previous implementation generated weekly sessions from a single
            // selected date. Remove only untouched scheduled copies; completed or
            // otherwise progressed historical sessions are deliberately preserved.
            migrationBuilder.Sql(
                """
                DELETE FROM "TrainingSessions" AS session
                USING "RoutineSchedules" AS schedule
                WHERE session."RoutineScheduleId" = schedule."RoutineScheduleId"
                  AND session."TrainingType" = 'Routine'
                  AND session."Status" = 'Scheduled'
                  AND session."SessionDate" <> schedule."EffectiveStartDate";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removed generated copies cannot be reconstructed safely because they
            // may have conflicted with schedules created after this migration.
        }
    }
}
