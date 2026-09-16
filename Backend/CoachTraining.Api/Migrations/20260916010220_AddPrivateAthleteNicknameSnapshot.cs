using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateAthleteNicknameSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AthleteNicknameSnapshot",
                table: "PrivateSessionAthletes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "PrivateSessionAthletes" AS psa
                SET "AthleteNicknameSnapshot" = NULLIF(BTRIM(a."Nickname"), '')
                FROM "Athletes" AS a
                WHERE psa."AthleteId" = a."AthleteId"
                  AND NOT psa."IsGuest";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AthleteNicknameSnapshot",
                table: "PrivateSessionAthletes");
        }
    }
}
