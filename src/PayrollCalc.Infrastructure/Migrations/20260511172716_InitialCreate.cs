using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PayrollCalc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvgSalaryInclusionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FieldKey = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    IncludeSick = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeVacation = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeTraining = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeCompensation = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvgSalaryInclusionRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotebookRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubjectKeyword = table.Column<string>(type: "text", nullable: false),
                    Pct = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotebookRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemParams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemParams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TariffGrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Grade = table.Column<int>(type: "integer", nullable: false),
                    MonthlyRate = table.Column<decimal>(type: "numeric", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TariffGrades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TitleTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Pct = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkCalendars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    WorkDays = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCalendars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false),
                    WorkerClass = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TabNumber = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DismissalDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Education = table.Column<string>(type: "text", nullable: true),
                    PedExperienceYears = table.Column<int>(type: "integer", nullable: false),
                    WorkerClass = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PositionId = table.Column<int>(type: "integer", nullable: false),
                    TitleTypeId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Employees_TitleTypes_TitleTypeId",
                        column: x => x.TitleTypeId,
                        principalTable: "TitleTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Calculations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    JSalary = table.Column<decimal>(type: "numeric", nullable: false),
                    NSalary = table.Column<decimal>(type: "numeric", nullable: false),
                    AllowancesTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    GpdTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    PkrTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    SickEmployer = table.Column<decimal>(type: "numeric", nullable: false),
                    SickFss = table.Column<decimal>(type: "numeric", nullable: false),
                    VacationAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TrainingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ManualTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    GrossSalary = table.Column<decimal>(type: "numeric", nullable: false),
                    Pdfo = table.Column<decimal>(type: "numeric", nullable: false),
                    Vz = table.Column<decimal>(type: "numeric", nullable: false),
                    UnionFee = table.Column<decimal>(type: "numeric", nullable: false),
                    NetSalary = table.Column<decimal>(type: "numeric", nullable: false),
                    Esv = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ParamsSnapshot = table.Column<string>(type: "text", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calculations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calculations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAdmins",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    DirectorPct = table.Column<decimal>(type: "numeric", nullable: false),
                    AdminRateCount = table.Column<decimal>(type: "numeric", nullable: false),
                    PedRateCount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAdmins", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_EmployeeAdmins_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAllowances",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    HasClassMgmt = table.Column<bool>(type: "boolean", nullable: false),
                    HasGym = table.Column<bool>(type: "boolean", nullable: false),
                    HasCabinet = table.Column<bool>(type: "boolean", nullable: false),
                    HasShootingRange = table.Column<bool>(type: "boolean", nullable: false),
                    HasComputers = table.Column<bool>(type: "boolean", nullable: false),
                    HasExtracurricular = table.Column<bool>(type: "boolean", nullable: false),
                    HasWebsite = table.Column<bool>(type: "boolean", nullable: false),
                    HasMentor = table.Column<bool>(type: "boolean", nullable: false),
                    MentorAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    HasLibraryMgmt = table.Column<bool>(type: "boolean", nullable: false),
                    LibraryMgmtAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    HasTextbooks = table.Column<bool>(type: "boolean", nullable: false),
                    TextbooksAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    HasUnfavorable = table.Column<bool>(type: "boolean", nullable: false),
                    HasMilitaryAcct = table.Column<bool>(type: "boolean", nullable: false),
                    ClassGradeGroup = table.Column<int>(type: "integer", nullable: true),
                    CabinetType = table.Column<int>(type: "integer", nullable: true)
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
                    RateCount = table.Column<decimal>(type: "numeric", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "EmployeeGpds",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    TariffGradeId = table.Column<int>(type: "integer", nullable: false),
                    GpdHours = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeGpds", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_EmployeeGpds_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeGpds_TariffGrades_TariffGradeId",
                        column: x => x.TariffGradeId,
                        principalTable: "TariffGrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeNonPedagogical",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    HasDisinfectants = table.Column<bool>(type: "boolean", nullable: false),
                    HasNightShifts = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeNonPedagogical", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_EmployeeNonPedagogical_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePkrs",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    PkrHours = table.Column<decimal>(type: "numeric", nullable: false),
                    TariffGradeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePkrs", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_EmployeePkrs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeePkrs_TariffGrades_TariffGradeId",
                        column: x => x.TariffGradeId,
                        principalTable: "TariffGrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWorkloads",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    Hours1To4 = table.Column<decimal>(type: "numeric", nullable: false),
                    IndividualHours1To4 = table.Column<decimal>(type: "numeric", nullable: false),
                    Hours5To9 = table.Column<decimal>(type: "numeric", nullable: false),
                    IndividualHours5To9 = table.Column<decimal>(type: "numeric", nullable: false),
                    Hours10To11 = table.Column<decimal>(type: "numeric", nullable: false),
                    IndividualHours10To11 = table.Column<decimal>(type: "numeric", nullable: false),
                    NotebookHours1To4 = table.Column<decimal>(type: "numeric", nullable: false),
                    NotebookHours5To9 = table.Column<decimal>(type: "numeric", nullable: false),
                    NotebookHours10To11 = table.Column<decimal>(type: "numeric", nullable: false),
                    InclusiveHours1To4 = table.Column<decimal>(type: "numeric", nullable: false),
                    InclusiveHours5To9 = table.Column<decimal>(type: "numeric", nullable: false),
                    NotebookRateId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeWorkloads", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkloads_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkloads_NotebookRates_NotebookRateId",
                        column: x => x.NotebookRateId,
                        principalTable: "NotebookRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnforcementDeductions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnforcementDeductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnforcementDeductions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SickLeaves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DaysTotal = table.Column<int>(type: "integer", nullable: false),
                    DaysEmployer = table.Column<int>(type: "integer", nullable: false),
                    DaysFss = table.Column<int>(type: "integer", nullable: false),
                    InsuranceSeniorityYrs = table.Column<int>(type: "integer", nullable: false),
                    PaymentPct = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseCalculationMode = table.Column<int>(type: "integer", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseExcludedDays = table.Column<int>(type: "integer", nullable: false),
                    BaseDays = table.Column<int>(type: "integer", nullable: false),
                    AverageDaily = table.Column<decimal>(type: "numeric", nullable: false),
                    AmountEmployer = table.Column<decimal>(type: "numeric", nullable: false),
                    AmountFss = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    EfssNumber = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SickLeaves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SickLeaves_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Timesheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    WorkedDays = table.Column<decimal>(type: "numeric", nullable: false),
                    NightHours = table.Column<decimal>(type: "numeric", nullable: false),
                    HolidayAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ReplacementHours = table.Column<decimal>(type: "numeric", nullable: false),
                    Recalculation = table.Column<decimal>(type: "numeric", nullable: false),
                    Advance = table.Column<decimal>(type: "numeric", nullable: false),
                    EnforcementOrders = table.Column<decimal>(type: "numeric", nullable: false),
                    AnnualBonus = table.Column<decimal>(type: "numeric", nullable: false),
                    OtherManual = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timesheets_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingLeaves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WorkingDaysAbsent = table.Column<int>(type: "integer", nullable: false),
                    BaseCalculationMode = table.Column<int>(type: "integer", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseWorkingDays = table.Column<int>(type: "integer", nullable: false),
                    AverageDaily = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    InstitutionName = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingLeaves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingLeaves_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vacations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    VacationType = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CalendarDays = table.Column<int>(type: "integer", nullable: false),
                    WorkingDaysAbsent = table.Column<int>(type: "integer", nullable: false),
                    BaseCalculationMode = table.Column<int>(type: "integer", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    BaseDays = table.Column<int>(type: "integer", nullable: true),
                    AverageDaily = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    IsCarryOver = table.Column<bool>(type: "boolean", nullable: false),
                    CarryOverYear = table.Column<int>(type: "integer", nullable: true),
                    CarryOverMonth = table.Column<int>(type: "integer", nullable: true),
                    OrderNumber = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vacations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vacations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalculationPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CalculationId = table.Column<int>(type: "integer", nullable: false),
                    DateFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    DateTo = table.Column<DateOnly>(type: "date", nullable: false),
                    WorkDays = table.Column<int>(type: "integer", nullable: false),
                    MonthlyRate = table.Column<decimal>(type: "numeric", nullable: false),
                    Bonus1749Pct = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculationPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalculationPeriods_Calculations_CalculationId",
                        column: x => x.CalculationId,
                        principalTable: "Calculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalculationPeriods_CalculationId",
                table: "CalculationPeriods",
                column: "CalculationId");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_EmployeeId",
                table: "Calculations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBases_TariffGradeId",
                table: "EmployeeBases",
                column: "TariffGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeGpds_TariffGradeId",
                table: "EmployeeGpds",
                column: "TariffGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePkrs_TariffGradeId",
                table: "EmployeePkrs",
                column: "TariffGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PositionId",
                table: "Employees",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_TitleTypeId",
                table: "Employees",
                column: "TitleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkloads_NotebookRateId",
                table: "EmployeeWorkloads",
                column: "NotebookRateId");

            migrationBuilder.CreateIndex(
                name: "IX_EnforcementDeductions_EmployeeId",
                table: "EnforcementDeductions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_DepartmentId",
                table: "Positions",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SickLeaves_EmployeeId",
                table: "SickLeaves",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_EmployeeId",
                table: "Timesheets",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLeaves_EmployeeId",
                table: "TrainingLeaves",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacations_EmployeeId",
                table: "Vacations",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvgSalaryInclusionRules");

            migrationBuilder.DropTable(
                name: "CalculationPeriods");

            migrationBuilder.DropTable(
                name: "EmployeeAdmins");

            migrationBuilder.DropTable(
                name: "EmployeeAllowances");

            migrationBuilder.DropTable(
                name: "EmployeeBases");

            migrationBuilder.DropTable(
                name: "EmployeeGpds");

            migrationBuilder.DropTable(
                name: "EmployeeNonPedagogical");

            migrationBuilder.DropTable(
                name: "EmployeePkrs");

            migrationBuilder.DropTable(
                name: "EmployeeWorkloads");

            migrationBuilder.DropTable(
                name: "EnforcementDeductions");

            migrationBuilder.DropTable(
                name: "SickLeaves");

            migrationBuilder.DropTable(
                name: "SystemParams");

            migrationBuilder.DropTable(
                name: "Timesheets");

            migrationBuilder.DropTable(
                name: "TrainingLeaves");

            migrationBuilder.DropTable(
                name: "Vacations");

            migrationBuilder.DropTable(
                name: "WorkCalendars");

            migrationBuilder.DropTable(
                name: "Calculations");

            migrationBuilder.DropTable(
                name: "TariffGrades");

            migrationBuilder.DropTable(
                name: "NotebookRates");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "TitleTypes");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
