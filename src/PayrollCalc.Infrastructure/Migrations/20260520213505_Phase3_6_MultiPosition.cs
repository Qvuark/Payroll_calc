using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PayrollCalc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_6_MultiPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAdmins_Employees_EmployeeId",
                table: "EmployeeAdmins");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeGpds_Employees_EmployeeId",
                table: "EmployeeGpds");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeGpds_TariffGrades_TariffGradeId",
                table: "EmployeeGpds");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeNonPedagogical_Employees_EmployeeId",
                table: "EmployeeNonPedagogical");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeePkrs_Employees_EmployeeId",
                table: "EmployeePkrs");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeePkrs_TariffGrades_TariffGradeId",
                table: "EmployeePkrs");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Positions_PositionId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeWorkloads_Employees_EmployeeId",
                table: "EmployeeWorkloads");

            migrationBuilder.DropTable(
                name: "EmployeeAllowances");

            migrationBuilder.DropTable(
                name: "EmployeeBases");

            migrationBuilder.DropIndex(
                name: "IX_Employees_PositionId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "EmployeeWorkloads",
                newName: "EmployeePositionId");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "EmployeePkrs",
                newName: "EmployeePositionId");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "EmployeeNonPedagogical",
                newName: "EmployeePositionId");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "EmployeeGpds",
                newName: "EmployeePositionId");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "EmployeeAdmins",
                newName: "EmployeePositionId");

            migrationBuilder.AddColumn<bool>(
                name: "HasComplexityBonus",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SocialBenefitPct",
                table: "Employees",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "Employees",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasLibraryMgmt",
                table: "EmployeeNonPedagogical",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasMentor",
                table: "EmployeeNonPedagogical",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasTextbooks",
                table: "EmployeeNonPedagogical",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LibraryMgmtAmount",
                table: "EmployeeNonPedagogical",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MentorAmount",
                table: "EmployeeNonPedagogical",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TextbooksAmount",
                table: "EmployeeNonPedagogical",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CabinetType",
                table: "EmployeeAdmins",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClassGradeGroup",
                table: "EmployeeAdmins",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasCabinet",
                table: "EmployeeAdmins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasClassMgmt",
                table: "EmployeeAdmins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasComputers",
                table: "EmployeeAdmins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasExtracurricular",
                table: "EmployeeAdmins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasGym",
                table: "EmployeeAdmins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasShootingRange",
                table: "EmployeeAdmins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasWebsite",
                table: "EmployeeAdmins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EmployeePositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    PositionId = table.Column<int>(type: "integer", nullable: false),
                    TariffGradeId = table.Column<int>(type: "integer", nullable: false),
                    RateCount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DismissalDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    HasMilitaryRecord = table.Column<bool>(type: "boolean", nullable: false),
                    HasUnfavorable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeePositions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeePositions_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePositions_TariffGrades_TariffGradeId",
                        column: x => x.TariffGradeId,
                        principalTable: "TariffGrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositions_EmployeeId_PositionId",
                table: "EmployeePositions",
                columns: new[] { "EmployeeId", "PositionId" },
                unique: true,
                filter: "\"DismissalDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositions_PositionId",
                table: "EmployeePositions",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositions_TariffGradeId",
                table: "EmployeePositions",
                column: "TariffGradeId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAdmins_EmployeePositions_EmployeePositionId",
                table: "EmployeeAdmins",
                column: "EmployeePositionId",
                principalTable: "EmployeePositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeGpds_EmployeePositions_EmployeePositionId",
                table: "EmployeeGpds",
                column: "EmployeePositionId",
                principalTable: "EmployeePositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeGpds_TariffGrades_TariffGradeId",
                table: "EmployeeGpds",
                column: "TariffGradeId",
                principalTable: "TariffGrades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeNonPedagogical_EmployeePositions_EmployeePositionId",
                table: "EmployeeNonPedagogical",
                column: "EmployeePositionId",
                principalTable: "EmployeePositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeePkrs_EmployeePositions_EmployeePositionId",
                table: "EmployeePkrs",
                column: "EmployeePositionId",
                principalTable: "EmployeePositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeePkrs_TariffGrades_TariffGradeId",
                table: "EmployeePkrs",
                column: "TariffGradeId",
                principalTable: "TariffGrades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeWorkloads_EmployeePositions_EmployeePositionId",
                table: "EmployeeWorkloads",
                column: "EmployeePositionId",
                principalTable: "EmployeePositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAdmins_EmployeePositions_EmployeePositionId",
                table: "EmployeeAdmins");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeGpds_EmployeePositions_EmployeePositionId",
                table: "EmployeeGpds");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeGpds_TariffGrades_TariffGradeId",
                table: "EmployeeGpds");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeNonPedagogical_EmployeePositions_EmployeePositionId",
                table: "EmployeeNonPedagogical");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeePkrs_EmployeePositions_EmployeePositionId",
                table: "EmployeePkrs");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeePkrs_TariffGrades_TariffGradeId",
                table: "EmployeePkrs");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeWorkloads_EmployeePositions_EmployeePositionId",
                table: "EmployeeWorkloads");

            migrationBuilder.DropTable(
                name: "EmployeePositions");

            migrationBuilder.DropColumn(
                name: "HasComplexityBonus",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SocialBenefitPct",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "HasLibraryMgmt",
                table: "EmployeeNonPedagogical");

            migrationBuilder.DropColumn(
                name: "HasMentor",
                table: "EmployeeNonPedagogical");

            migrationBuilder.DropColumn(
                name: "HasTextbooks",
                table: "EmployeeNonPedagogical");

            migrationBuilder.DropColumn(
                name: "LibraryMgmtAmount",
                table: "EmployeeNonPedagogical");

            migrationBuilder.DropColumn(
                name: "MentorAmount",
                table: "EmployeeNonPedagogical");

            migrationBuilder.DropColumn(
                name: "TextbooksAmount",
                table: "EmployeeNonPedagogical");

            migrationBuilder.DropColumn(
                name: "CabinetType",
                table: "EmployeeAdmins");

            migrationBuilder.DropColumn(
                name: "ClassGradeGroup",
                table: "EmployeeAdmins");

            migrationBuilder.DropColumn(
                name: "HasCabinet",
                table: "EmployeeAdmins");

            migrationBuilder.DropColumn(
                name: "HasClassMgmt",
                table: "EmployeeAdmins");

            migrationBuilder.DropColumn(
                name: "HasComputers",
                table: "EmployeeAdmins");

            migrationBuilder.DropColumn(
                name: "HasExtracurricular",
                table: "EmployeeAdmins");

            migrationBuilder.DropColumn(
                name: "HasGym",
                table: "EmployeeAdmins");

            migrationBuilder.DropColumn(
                name: "HasShootingRange",
                table: "EmployeeAdmins");

            migrationBuilder.DropColumn(
                name: "HasWebsite",
                table: "EmployeeAdmins");

            migrationBuilder.RenameColumn(
                name: "EmployeePositionId",
                table: "EmployeeWorkloads",
                newName: "EmployeeId");

            migrationBuilder.RenameColumn(
                name: "EmployeePositionId",
                table: "EmployeePkrs",
                newName: "EmployeeId");

            migrationBuilder.RenameColumn(
                name: "EmployeePositionId",
                table: "EmployeeNonPedagogical",
                newName: "EmployeeId");

            migrationBuilder.RenameColumn(
                name: "EmployeePositionId",
                table: "EmployeeGpds",
                newName: "EmployeeId");

            migrationBuilder.RenameColumn(
                name: "EmployeePositionId",
                table: "EmployeeAdmins",
                newName: "EmployeeId");

            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "Employees",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EmployeeAllowances",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    CabinetType = table.Column<int>(type: "integer", nullable: true),
                    ClassGradeGroup = table.Column<int>(type: "integer", nullable: true),
                    HasCabinet = table.Column<bool>(type: "boolean", nullable: false),
                    HasClassMgmt = table.Column<bool>(type: "boolean", nullable: false),
                    HasComputers = table.Column<bool>(type: "boolean", nullable: false),
                    HasExtracurricular = table.Column<bool>(type: "boolean", nullable: false),
                    HasGym = table.Column<bool>(type: "boolean", nullable: false),
                    HasLibraryMgmt = table.Column<bool>(type: "boolean", nullable: false),
                    HasMentor = table.Column<bool>(type: "boolean", nullable: false),
                    HasMilitaryAcct = table.Column<bool>(type: "boolean", nullable: false),
                    HasShootingRange = table.Column<bool>(type: "boolean", nullable: false),
                    HasTextbooks = table.Column<bool>(type: "boolean", nullable: false),
                    HasUnfavorable = table.Column<bool>(type: "boolean", nullable: false),
                    HasWebsite = table.Column<bool>(type: "boolean", nullable: false),
                    LibraryMgmtAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MentorAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TextbooksAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAllowances", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_EmployeeAllowances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeBases",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    TariffGradeId = table.Column<int>(type: "integer", nullable: false),
                    RateCount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeBases", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_EmployeeBases_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeBases_TariffGrades_TariffGradeId",
                        column: x => x.TariffGradeId,
                        principalTable: "TariffGrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PositionId",
                table: "Employees",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBases_TariffGradeId",
                table: "EmployeeBases",
                column: "TariffGradeId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAdmins_Employees_EmployeeId",
                table: "EmployeeAdmins",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeGpds_Employees_EmployeeId",
                table: "EmployeeGpds",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeGpds_TariffGrades_TariffGradeId",
                table: "EmployeeGpds",
                column: "TariffGradeId",
                principalTable: "TariffGrades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeNonPedagogical_Employees_EmployeeId",
                table: "EmployeeNonPedagogical",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeePkrs_Employees_EmployeeId",
                table: "EmployeePkrs",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeePkrs_TariffGrades_TariffGradeId",
                table: "EmployeePkrs",
                column: "TariffGradeId",
                principalTable: "TariffGrades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Positions_PositionId",
                table: "Employees",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeWorkloads_Employees_EmployeeId",
                table: "EmployeeWorkloads",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
