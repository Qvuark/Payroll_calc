using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;

namespace PayrollCalc.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<TitleType> TitleTypes => Set<TitleType>();
    public DbSet<TariffGrade> TariffGrades => Set<TariffGrade>();
    public DbSet<CalculationPeriod> CalculationPeriods => Set<CalculationPeriod>();
    public DbSet<Calculation> Calculations => Set<Calculation>();
    public DbSet<EmployeeWorkload> EmployeeWorkloads => Set<EmployeeWorkload>();
    public DbSet<EmployeeGpd> EmployeeGpds => Set<EmployeeGpd>();
    public DbSet<EmployeeNonPedagogical> EmployeeNonPedagogical => Set<EmployeeNonPedagogical>();
    public DbSet<SickLeave> SickLeaves => Set<SickLeave>();
    public DbSet<Vacation> Vacations => Set<Vacation>();
    public DbSet<TrainingLeave> TrainingLeaves => Set<TrainingLeave>();
    public DbSet<AvgSalaryInclusionRule> AvgSalaryInclusionRules => Set<AvgSalaryInclusionRule>();
    public DbSet<EnforcementDeduction> EnforcementDeductions => Set<EnforcementDeduction>();
    public DbSet<NotebookRate> NotebookRates => Set<NotebookRate>();
    public DbSet<EmployeeAdmin> EmployeeAdmins => Set<EmployeeAdmin>();
    public DbSet<EmployeePkr> EmployeePkrs => Set<EmployeePkr>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<SystemParam> SystemParams => Set<SystemParam>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<EmployeePosition> EmployeePositions => Set<EmployeePosition>();
    public DbSet<WorkCalendar> WorkCalendars => Set<WorkCalendar>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // PK блоків — composite на EmployeePositionId (1-to-1 з EmployeePosition)
        modelBuilder.Entity<EmployeeWorkload>().HasKey(x => x.EmployeePositionId);
        modelBuilder.Entity<EmployeeAdmin>().HasKey(x => x.EmployeePositionId);
        modelBuilder.Entity<EmployeeGpd>().HasKey(x => x.EmployeePositionId);
        modelBuilder.Entity<EmployeePkr>().HasKey(x => x.EmployeePositionId);
        modelBuilder.Entity<EmployeeNonPedagogical>().HasKey(x => x.EmployeePositionId);
        // PK інших сутностей
        modelBuilder.Entity<SickLeave>().HasKey(x => x.Id);
        modelBuilder.Entity<Vacation>().HasKey(x => x.Id);
        modelBuilder.Entity<TrainingLeave>().HasKey(x => x.Id);
        modelBuilder.Entity<Calculation>().HasKey(x => x.Id);
        modelBuilder.Entity<Timesheet>().HasKey(x => x.Id);
        modelBuilder.Entity<SystemParam>().HasKey(x => x.Id);
        modelBuilder.Entity<WorkCalendar>().HasKey(x => x.Id);

        modelBuilder.Entity<EmployeePosition>().HasOne(ep => ep.Employee).WithMany(e => e.Positions).HasForeignKey(ep => ep.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmployeePosition>().HasOne(ep => ep.Position).WithMany().HasForeignKey(ep => ep.PositionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EmployeePosition>().HasOne(ep => ep.TariffGrade).WithMany().HasForeignKey(ep => ep.TariffGradeId).OnDelete(DeleteBehavior.Restrict);
        // Унікальна активна ставка: одна (Employee, Position) серед записів де DismissalDate IS NULL
        modelBuilder.Entity<EmployeePosition>().HasIndex(ep => new { ep.EmployeeId, ep.PositionId }).IsUnique().HasFilter("\"DismissalDate\" IS NULL");

        // Блоки 1-to-1 з EmployeePosition (Cascade — блок мертвий без ставки)
        modelBuilder.Entity<EmployeeWorkload>().HasOne(w => w.EmployeePosition).WithOne(ep => ep.Workload).HasForeignKey<EmployeeWorkload>(w => w.EmployeePositionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmployeeAdmin>().HasOne(a => a.EmployeePosition).WithOne(ep => ep.Admin).HasForeignKey<EmployeeAdmin>(a => a.EmployeePositionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmployeeGpd>().HasOne(g => g.EmployeePosition).WithOne(ep => ep.Gpd).HasForeignKey<EmployeeGpd>(g => g.EmployeePositionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmployeePkr>().HasOne(p => p.EmployeePosition).WithOne(ep => ep.Pkr).HasForeignKey<EmployeePkr>(p => p.EmployeePositionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmployeeNonPedagogical>().HasOne(n => n.EmployeePosition).WithOne(ep => ep.NonPedagogical).HasForeignKey<EmployeeNonPedagogical>(n => n.EmployeePositionId).OnDelete(DeleteBehavior.Cascade);

        // Власні розряди ГПД/ПКР (Restrict — не можна видалити TariffGrade зі справочника якщо є ставки)
        modelBuilder.Entity<EmployeeGpd>().HasOne(g => g.TariffGrade).WithMany().HasForeignKey(g => g.TariffGradeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EmployeePkr>().HasOne(p => p.TariffGrade).WithMany().HasForeignKey(p => p.TariffGradeId).OnDelete(DeleteBehavior.Restrict);

        // Position + Employee — без блочних навігацій (вони на EmployeePosition)
        modelBuilder.Entity<Position>().HasOne(p => p.Department).WithMany().HasForeignKey(p => p.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Position>().Property(p => p.ExcelAliases).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        modelBuilder.Entity<TitleType>().Property(t => t.ExcelAliases).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        modelBuilder.Entity<EmployeePosition>().HasOne(p => p.TitleType).WithMany().HasForeignKey(p => p.TitleTypeId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Employee>().Property(e => e.TaxId).HasMaxLength(10).IsRequired();

        // Інші Employee-залежні сутності (без змін у Phase 3.6 — прив'язані до Employee, не до EmployeePosition)
        modelBuilder.Entity<SickLeave>().HasOne(s => s.Employee).WithMany().OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Vacation>().HasOne(v => v.Employee).WithMany().OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TrainingLeave>().HasOne(t => t.Employee).WithMany().OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Calculation>().HasOne(c => c.Employee).WithMany().OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CalculationPeriod>().HasOne(cp => cp.Calculation).WithMany(c => c.Periods).HasForeignKey(cp => cp.CalculationId).OnDelete(DeleteBehavior.Cascade);

        // Унікальні індекси
        modelBuilder.Entity<Employee>().HasIndex(e => e.TaxId).IsUnique();
        modelBuilder.Entity<SystemParam>().HasIndex(e => e.Key).IsUnique();
        modelBuilder.Entity<TariffGrade>().HasIndex(e => e.Grade).IsUnique();
        modelBuilder.Entity<WorkCalendar>().HasIndex(e => new { e.Year, e.Month }).IsUnique();
        modelBuilder.Entity<Timesheet>().HasIndex(e => new { e.EmployeeId, e.Year, e.Month }).IsUnique();
        modelBuilder.Entity<Calculation>().HasIndex(e => new { e.EmployeeId, e.Year, e.Month }).IsUnique();
        // Довідники — імена унікальні, бо резолвери імпорту шукають за назвою (FirstOrDefault by Name).
        // Дублі дали б недетермінований вибір рядка → тихий баг у розрахунку.
        modelBuilder.Entity<Position>().HasIndex(p => p.Name).IsUnique();
        modelBuilder.Entity<TitleType>().HasIndex(t => new { t.Name, t.WorkerClass }).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(d => d.Name).IsUnique();
        // FieldKey — ключ правила включення у середню; дублі зробили б розрахунок недетермінованим.
        modelBuilder.Entity<AvgSalaryInclusionRule>().HasIndex(r => r.FieldKey).IsUnique();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
    }
}
