using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_6_5_MomReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_TabNumber",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "HasComplexityBonus",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AdminRateCount",
                table: "EmployeeAdmins");

            migrationBuilder.DropColumn(
                name: "DirectorPct",
                table: "EmployeeAdmins");

            migrationBuilder.DropColumn(
                name: "PedRateCount",
                table: "EmployeeAdmins");

            migrationBuilder.AddColumn<int>(
                name: "WorkerClass",
                table: "TitleTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "InclusiveHours10To11",
                table: "EmployeeWorkloads",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "TaxId",
                table: "Employees",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GeneralExperienceYears",
                table: "Employees",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ComplexityBonusPct",
                table: "EmployeePositions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrestigeBonusPct",
                table: "EmployeePositions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_TaxId",
                table: "Employees",
                column: "TaxId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_TaxId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "WorkerClass",
                table: "TitleTypes");

            migrationBuilder.DropColumn(
                name: "InclusiveHours10To11",
                table: "EmployeeWorkloads");

            migrationBuilder.DropColumn(
                name: "GeneralExperienceYears",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ComplexityBonusPct",
                table: "EmployeePositions");

            migrationBuilder.DropColumn(
                name: "PrestigeBonusPct",
                table: "EmployeePositions");

            migrationBuilder.AlterColumn<string>(
                name: "TaxId",
                table: "Employees",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<bool>(
                name: "HasComplexityBonus",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "AdminRateCount",
                table: "EmployeeAdmins",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DirectorPct",
                table: "EmployeeAdmins",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PedRateCount",
                table: "EmployeeAdmins",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_TabNumber",
                table: "Employees",
                column: "TabNumber",
                unique: true);
        }
    }
}
