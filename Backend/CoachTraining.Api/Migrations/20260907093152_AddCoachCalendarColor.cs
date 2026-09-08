using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachCalendarColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "Coaches",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#10B981");

            migrationBuilder.Sql("""
                UPDATE "Coaches"
                SET "ColorHex" = CASE MOD("CoachId", 8)
                    WHEN 0 THEN '#10B981'
                    WHEN 1 THEN '#0EA5E9'
                    WHEN 2 THEN '#8B5CF6'
                    WHEN 3 THEN '#F59E0B'
                    WHEN 4 THEN '#F43F5E'
                    WHEN 5 THEN '#06B6D4'
                    WHEN 6 THEN '#84CC16'
                    ELSE '#F97316'
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "Coaches");
        }
    }
}
