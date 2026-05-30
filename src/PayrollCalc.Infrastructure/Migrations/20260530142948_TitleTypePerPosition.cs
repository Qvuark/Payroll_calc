using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TitleTypePerPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Звання переїжджає Employee → EmployeePosition (per-position scope).
            // Порядок критичний: спершу додаємо нову колонку, переносимо дані на головну ставку,
            // і ТІЛЬКИ потім дропаємо стару — інакше звання було б втрачено.
            migrationBuilder.AddColumn<int>(
                name: "TitleTypeId",
                table: "EmployeePositions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositions_TitleTypeId",
                table: "EmployeePositions",
                column: "TitleTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeePositions_TitleTypes_TitleTypeId",
                table: "EmployeePositions",
                column: "TitleTypeId",
                principalTable: "TitleTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Backfill: звання людини переносимо на її головну ставку (IsPrimary=true).
            // Якщо primary немає — звання осиротіло б, тому беремо будь-яку ставку з найменшим Id як fallback.
            migrationBuilder.Sql(@"
                UPDATE ""EmployeePositions"" ep
                SET ""TitleTypeId"" = e.""TitleTypeId""
                FROM ""Employees"" e
                WHERE ep.""EmployeeId"" = e.""Id""
                  AND e.""TitleTypeId"" IS NOT NULL
                  AND ep.""Id"" = (
                      SELECT inner_ep.""Id"" FROM ""EmployeePositions"" inner_ep
                      WHERE inner_ep.""EmployeeId"" = e.""Id""
                      ORDER BY inner_ep.""IsPrimary"" DESC, inner_ep.""Id"" ASC
                      LIMIT 1
                  );");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_TitleTypes_TitleTypeId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_TitleTypeId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TitleTypeId",
                table: "Employees");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeePositions_TitleTypes_TitleTypeId",
                table: "EmployeePositions");

            migrationBuilder.DropIndex(
                name: "IX_EmployeePositions_TitleTypeId",
                table: "EmployeePositions");

            migrationBuilder.DropColumn(
                name: "TitleTypeId",
                table: "EmployeePositions");

            migrationBuilder.AddColumn<int>(
                name: "TitleTypeId",
                table: "Employees",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_TitleTypeId",
                table: "Employees",
                column: "TitleTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_TitleTypes_TitleTypeId",
                table: "Employees",
                column: "TitleTypeId",
                principalTable: "TitleTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
