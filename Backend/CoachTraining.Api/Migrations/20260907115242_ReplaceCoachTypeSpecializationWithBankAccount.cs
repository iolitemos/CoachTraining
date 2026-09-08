using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachTraining.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceCoachTypeSpecializationWithBankAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop-and-add rather than rename: CoachType/Specialization values carry no
            // valid meaning as bank-account data, so old values must not survive as
            // misleadingly-relabelled columns (CLAUDE.md 4.5 covers historical business
            // records, not stale profile-field values like this).
            migrationBuilder.DropColumn(
                name: "CoachType",
                table: "Coaches");

            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "Coaches");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Coaches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Coaches",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountName",
                table: "Coaches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Coaches");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Coaches");

            migrationBuilder.DropColumn(
                name: "BankAccountName",
                table: "Coaches");

            migrationBuilder.AddColumn<string>(
                name: "CoachType",
                table: "Coaches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "Coaches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
