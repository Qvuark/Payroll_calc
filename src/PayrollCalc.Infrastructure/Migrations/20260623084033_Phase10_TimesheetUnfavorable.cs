using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase10_TimesheetUnfavorable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnfavorableManual",
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
                name: "UnfavorableManual",
                table: "Timesheets");
        }
    }
}
