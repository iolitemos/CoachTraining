using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutineScheduleApprovalRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoutineScheduleRequests",
                columns: table => new
                {
                    RoutineScheduleRequestId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubmissionBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachId = table.Column<int>(type: "integer", nullable: false),
                    ProposedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ProposedStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ProposedEndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ProposedRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ApprovedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ApprovedStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    ApprovedEndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    ApprovedRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RoutineScheduleId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineScheduleRequests", x => x.RoutineScheduleRequestId);
                    table.ForeignKey(
                        name: "FK_RoutineScheduleRequests_Coaches_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Coaches",
                        principalColumn: "CoachId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutineScheduleRequests_RoutineSchedules_RoutineScheduleId",
                        column: x => x.RoutineScheduleId,
                        principalTable: "RoutineSchedules",
                        principalColumn: "RoutineScheduleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutineScheduleRequests_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoutineScheduleRequests_CoachId_ProposedDate",
                table: "RoutineScheduleRequests",
                columns: new[] { "CoachId", "ProposedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RoutineScheduleRequests_ReviewedByUserId",
                table: "RoutineScheduleRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineScheduleRequests_RoutineScheduleId",
                table: "RoutineScheduleRequests",
                column: "RoutineScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineScheduleRequests_Status_ProposedDate",
                table: "RoutineScheduleRequests",
                columns: new[] { "Status", "ProposedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RoutineScheduleRequests_SubmissionBatchId",
                table: "RoutineScheduleRequests",
                column: "SubmissionBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoutineScheduleRequests");
        }
    }
}
