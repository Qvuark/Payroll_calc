using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase7_TimesheetExtraManualColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "Downtime",
                table: "Timesheets",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Indexation",
                table: "Timesheets",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PhysEducation",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Courses",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "Downtime",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "Indexation",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "PhysEducation",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "VacationCompensation",
                table: "Timesheets");
        }
    }
}
