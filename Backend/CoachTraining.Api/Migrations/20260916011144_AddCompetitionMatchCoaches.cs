using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionMatchCoaches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompetitionMatchCoaches",
                columns: table => new
                {
                    CompetitionMatchCoachId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompetitionMatchId = table.Column<int>(type: "integer", nullable: false),
                    CoachId = table.Column<int>(type: "integer", nullable: false),
                    CoachNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CoachNicknameSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionMatchCoaches", x => x.CompetitionMatchCoachId);
                    table.ForeignKey(
                        name: "FK_CompetitionMatchCoaches_Coaches_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Coaches",
                        principalColumn: "CoachId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompetitionMatchCoaches_CompetitionMatches_CompetitionMatch~",
                        column: x => x.CompetitionMatchId,
                        principalTable: "CompetitionMatches",
                        principalColumn: "CompetitionMatchId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionMatchCoaches_CoachId",
                table: "CompetitionMatchCoaches",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionMatchCoaches_CompetitionMatchId_CoachId",
                table: "CompetitionMatchCoaches",
                columns: new[] { "CompetitionMatchId", "CoachId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetitionMatchCoaches");
        }
    }
}
