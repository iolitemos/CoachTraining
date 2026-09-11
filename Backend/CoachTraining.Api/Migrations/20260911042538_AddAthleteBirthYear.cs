using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAthleteBirthYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BirthYear",
                table: "Athletes",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Athletes"
                SET "BirthYear" = EXTRACT(YEAR FROM "DateOfBirth")::integer
                WHERE "DateOfBirth" IS NOT NULL AND "BirthYear" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthYear",
                table: "Athletes");
        }
    }
}
