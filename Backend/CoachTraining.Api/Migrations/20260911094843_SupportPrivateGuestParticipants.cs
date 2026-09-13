using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class SupportPrivateGuestParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AthleteId",
                table: "PrivateSessionAthletes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "GuestPhone",
                table: "PrivateSessionAthletes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestRemark",
                table: "PrivateSessionAthletes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGuest",
                table: "PrivateSessionAthletes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "AthleteId",
                table: "Attendances",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "PrivateSessionAthleteId",
                table: "Attendances",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Attendances" AS attendance
                SET "PrivateSessionAthleteId" = participant."PrivateSessionAthleteId"
                FROM "PrivateSessionAthletes" AS participant
                WHERE attendance."TrainingSessionId" = participant."TrainingSessionId"
                  AND attendance."AthleteId" = participant."AthleteId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_PrivateSessionAthleteId",
                table: "Attendances",
                column: "PrivateSessionAthleteId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_PrivateSessionAthletes_PrivateSessionAthleteId",
                table: "Attendances",
                column: "PrivateSessionAthleteId",
                principalTable: "PrivateSessionAthletes",
                principalColumn: "PrivateSessionAthleteId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "Attendances" WHERE "AthleteId" IS NULL;
                DELETE FROM "PrivateSessionAthletes" WHERE "AthleteId" IS NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_PrivateSessionAthletes_PrivateSessionAthleteId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_PrivateSessionAthleteId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "GuestPhone",
                table: "PrivateSessionAthletes");

            migrationBuilder.DropColumn(
                name: "GuestRemark",
                table: "PrivateSessionAthletes");

            migrationBuilder.DropColumn(
                name: "IsGuest",
                table: "PrivateSessionAthletes");

            migrationBuilder.DropColumn(
                name: "PrivateSessionAthleteId",
                table: "Attendances");

            migrationBuilder.AlterColumn<int>(
                name: "AthleteId",
                table: "PrivateSessionAthletes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AthleteId",
                table: "Attendances",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
