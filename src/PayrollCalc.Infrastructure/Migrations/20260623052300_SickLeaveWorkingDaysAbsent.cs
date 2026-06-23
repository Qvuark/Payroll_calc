using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SickLeaveWorkingDaysAbsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkingDaysAbsent",
                table: "SickLeaves",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkingDaysAbsent",
                table: "SickLeaves");
        }
    }
}
