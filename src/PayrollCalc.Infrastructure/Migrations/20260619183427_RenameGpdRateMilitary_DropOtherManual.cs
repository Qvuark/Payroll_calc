using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameGpdRateMilitary_DropOtherManual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtherManual",
                table: "Timesheets");

            migrationBuilder.RenameColumn(
                name: "HasMilitaryRecord",
                table: "EmployeePositions",
                newName: "MaintainsMilitaryRecords");

            migrationBuilder.RenameColumn(
                name: "GpdHours",
                table: "EmployeeGpds",
                newName: "GpdRate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaintainsMilitaryRecords",
                table: "EmployeePositions",
                newName: "HasMilitaryRecord");

            migrationBuilder.RenameColumn(
                name: "GpdRate",
                table: "EmployeeGpds",
                newName: "GpdHours");

            migrationBuilder.AddColumn<decimal>(
                name: "OtherManual",
                table: "Timesheets",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
