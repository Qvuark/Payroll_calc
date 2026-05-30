using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_7_MediumFixes_AvgSalaryRuleUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AvgSalaryInclusionRules_FieldKey",
                table: "AvgSalaryInclusionRules",
                column: "FieldKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AvgSalaryInclusionRules_FieldKey",
                table: "AvgSalaryInclusionRules");
        }
    }
}
