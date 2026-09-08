using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRoutineScheduleManagedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve the original weekday meaning by moving EffectiveStartDate
            // to the first actual occurrence before DayOfWeek is removed.
            migrationBuilder.Sql("""
                UPDATE "RoutineSchedules"
                SET "EffectiveStartDate" = "EffectiveStartDate"
                    + ((("DayOfWeek" - EXTRACT(DOW FROM "EffectiveStartDate")::int) + 7) % 7);

                UPDATE "RoutineSchedules"
                SET "IsActive" = FALSE
                WHERE "EffectiveEndDate" IS NOT NULL
                  AND "EffectiveEndDate" < CURRENT_DATE;
                """);

            migrationBuilder.DropIndex(
                name: "IX_RoutineSchedules_CoachId_DayOfWeek_IsActive",
                table: "RoutineSchedules");

            migrationBuilder.DropColumn(
                name: "DayOfWeek",
                table: "RoutineSchedules");

            migrationBuilder.DropColumn(
                name: "EffectiveEndDate",
                table: "RoutineSchedules");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "RoutineSchedules");

            migrationBuilder.DropColumn(
                name: "RecurrencePattern",
                table: "RoutineSchedules");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineSchedules_CoachId_EffectiveStartDate_IsActive",
                table: "RoutineSchedules",
                columns: new[] { "CoachId", "EffectiveStartDate", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoutineSchedules_CoachId_EffectiveStartDate_IsActive",
                table: "RoutineSchedules");

            migrationBuilder.AddColumn<int>(
                name: "DayOfWeek",
                table: "RoutineSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveEndDate",
                table: "RoutineSchedules",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "RoutineSchedules",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecurrencePattern",
                table: "RoutineSchedules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "RoutineSchedules"
                SET "DayOfWeek" = EXTRACT(DOW FROM "EffectiveStartDate")::int,
                    "Name" = 'Routine Training ' || TO_CHAR("StartTime", 'HH24:MI') || '-' || TO_CHAR("EndTime", 'HH24:MI'),
                    "RecurrencePattern" = 'Weekly';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_RoutineSchedules_CoachId_DayOfWeek_IsActive",
                table: "RoutineSchedules",
                columns: new[] { "CoachId", "DayOfWeek", "IsActive" });
        }
    }
}
