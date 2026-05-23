using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_7_ParsersPrep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "ExcelAliases",
                table: "TitleTypes",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<decimal>(
                name: "HonoredAmount",
                table: "Employees",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHonored",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveTo",
                table: "EmployeePositions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PositionStartDate",
                table: "EmployeePositions",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcelAliases",
                table: "TitleTypes");

            migrationBuilder.DropColumn(
                name: "HonoredAmount",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsHonored",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "EmployeePositions");

            migrationBuilder.DropColumn(
                name: "PositionStartDate",
                table: "EmployeePositions");
        }
    }
}
