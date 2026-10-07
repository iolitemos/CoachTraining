using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class RepairStaleActualCoachAfterReassignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A Scheduled Private session edited to a different Assigned Coach
            // previously retained the substitute/Actual Coach from the old
            // assignment. Record and clear only rows whose latest substitution
            // history proves that the assignment changed afterward.
            migrationBuilder.Sql(
                """
                WITH latest_substitution AS (
                    SELECT DISTINCT ON (h."TrainingSessionId")
                        h."TrainingSessionId",
                        h."OriginalCoachId",
                        h."SubstituteCoachId"
                    FROM "CoachSubstitutionHistories" h
                    ORDER BY h."TrainingSessionId", h."ActionDate" DESC, h."CoachSubstitutionHistoryId" DESC
                ),
                stale_sessions AS (
                    SELECT
                        ts."TrainingSessionId",
                        ts."AssignedCoachId",
                        ts."AssignedCoachCodeSnapshot",
                        ts."AssignedCoachNameSnapshot",
                        ts."ActualCoachId",
                        ts."ActualCoachCodeSnapshot",
                        ts."ActualCoachNameSnapshot",
                        COALESCE(ts."UpdatedByUserId", ts."CreatedByUserId") AS "ActionByUserId"
                    FROM "TrainingSessions" ts
                    INNER JOIN latest_substitution latest
                        ON latest."TrainingSessionId" = ts."TrainingSessionId"
                    WHERE ts."TrainingType" = 'Private'
                      AND ts."Status" = 'Scheduled'
                      AND ts."IsDeleted" = FALSE
                      AND ts."ActualCoachId" = latest."SubstituteCoachId"
                      AND ts."AssignedCoachId" <> latest."OriginalCoachId"
                )
                INSERT INTO "AuditLogs" (
                    "EntityName",
                    "EntityId",
                    "Action",
                    "PreviousValue",
                    "NewValue",
                    "ActionByUserId",
                    "ActionDate")
                SELECT
                    'TrainingSession',
                    stale."TrainingSessionId",
                    'RepairStaleActualCoach',
                    jsonb_build_object(
                        'AssignedCoachId', stale."AssignedCoachId",
                        'AssignedCoachCode', stale."AssignedCoachCodeSnapshot",
                        'AssignedCoachName', stale."AssignedCoachNameSnapshot",
                        'ActualCoachId', stale."ActualCoachId",
                        'ActualCoachCode', stale."ActualCoachCodeSnapshot",
                        'ActualCoachName', stale."ActualCoachNameSnapshot")::text,
                    jsonb_build_object(
                        'AssignedCoachId', stale."AssignedCoachId",
                        'ActualCoachId', NULL)::text,
                    stale."ActionByUserId",
                    CURRENT_TIMESTAMP
                FROM stale_sessions stale
                INNER JOIN "Users" u ON u."UserId" = stale."ActionByUserId";

                WITH latest_substitution AS (
                    SELECT DISTINCT ON (h."TrainingSessionId")
                        h."TrainingSessionId",
                        h."OriginalCoachId",
                        h."SubstituteCoachId"
                    FROM "CoachSubstitutionHistories" h
                    ORDER BY h."TrainingSessionId", h."ActionDate" DESC, h."CoachSubstitutionHistoryId" DESC
                )
                UPDATE "TrainingSessions" ts
                SET
                    "ActualCoachId" = NULL,
                    "ActualCoachCodeSnapshot" = NULL,
                    "ActualCoachNameSnapshot" = NULL,
                    "UpdatedDate" = CURRENT_TIMESTAMP
                FROM latest_substitution latest
                WHERE latest."TrainingSessionId" = ts."TrainingSessionId"
                  AND ts."TrainingType" = 'Private'
                  AND ts."Status" = 'Scheduled'
                  AND ts."IsDeleted" = FALSE
                  AND ts."ActualCoachId" = latest."SubstituteCoachId"
                  AND ts."AssignedCoachId" <> latest."OriginalCoachId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally preserve repaired assignments and their audit trail.
        }
    }
}
