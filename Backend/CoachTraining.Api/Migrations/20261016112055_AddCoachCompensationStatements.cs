using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachCompensationStatements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrivateRateSnapshot",
                table: "TrainingSessions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CoachCompensationStatements",
                columns: table => new
                {
                    CoachCompensationStatementId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CoachId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    BaseSalarySnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustmentAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustmentReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConfirmedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedByUserId = table.Column<int>(type: "integer", nullable: true),
                    PaidDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachCompensationStatements", x => x.CoachCompensationStatementId);
                    table.ForeignKey(
                        name: "FK_CoachCompensationStatements_Coaches_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Coaches",
                        principalColumn: "CoachId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoachCompensationItems",
                columns: table => new
                {
                    CoachCompensationItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CoachCompensationStatementId = table.Column<int>(type: "integer", nullable: false),
                    TrainingSessionId = table.Column<int>(type: "integer", nullable: false),
                    SessionDateSnapshot = table.Column<DateOnly>(type: "date", nullable: false),
                    ScheduledStartSnapshot = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ScheduledEndSnapshot = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CoachNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParticipantNamesSnapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PrivateRateSnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachCompensationItems", x => x.CoachCompensationItemId);
                    table.ForeignKey(
                        name: "FK_CoachCompensationItems_CoachCompensationStatements_CoachCom~",
                        column: x => x.CoachCompensationStatementId,
                        principalTable: "CoachCompensationStatements",
                        principalColumn: "CoachCompensationStatementId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachCompensationItems_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "TrainingSessionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachCompensationItems_CoachCompensationStatementId",
                table: "CoachCompensationItems",
                column: "CoachCompensationStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachCompensationItems_TrainingSessionId",
                table: "CoachCompensationItems",
                column: "TrainingSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachCompensationStatements_CoachId_Year_Month",
                table: "CoachCompensationStatements",
                columns: new[] { "CoachId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachCompensationStatements_Year_Month_Status",
                table: "CoachCompensationStatements",
                columns: new[] { "Year", "Month", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachCompensationItems");

            migrationBuilder.DropTable(
                name: "CoachCompensationStatements");

            migrationBuilder.DropColumn(
                name: "PrivateRateSnapshot",
                table: "TrainingSessions");
        }
    }
}
