using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AvgSalaryOverrideAmounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OverrideTotalAmount",
                table: "Vacations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OverrideTotalAmount",
                table: "TrainingLeaves",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OverrideAmountEmployer",
                table: "SickLeaves",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OverrideAmountFss",
                table: "SickLeaves",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OverrideTotalAmount",
                table: "Vacations");

            migrationBuilder.DropColumn(
                name: "OverrideTotalAmount",
                table: "TrainingLeaves");

            migrationBuilder.DropColumn(
                name: "OverrideAmountEmployer",
                table: "SickLeaves");

            migrationBuilder.DropColumn(
                name: "OverrideAmountFss",
                table: "SickLeaves");
        }
    }
}
