using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropTimesheetAbsenceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Courses",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "SickEmployer",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "SickFss",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "Vacation",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "VacationCompensation",
                table: "Timesheets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Courses",
                table: "Timesheets",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SickEmployer",
                table: "Timesheets",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SickFss",
                table: "Timesheets",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Vacation",
                table: "Timesheets",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VacationCompensation",
                table: "Timesheets",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
