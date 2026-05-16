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
    public DbSet<EmployeeAllowances> EmployeeAllowances => Set<EmployeeAllowances>();
    public DbSet<EmployeeGpd> EmployeeGpds => Set<EmployeeGpd>();
    public DbSet<EmployeeNonPedagogical> EmployeeNonPedagogical => Set<EmployeeNonPedagogical>();
    public DbSet<SickLeave> SickLeaves => Set<SickLeave>();
    public DbSet<Vacation> Vacations => Set<Vacation>();
    public DbSet<TrainingLeave> TrainingLeaves => Set<TrainingLeave>();
    public DbSet<AvgSalaryInclusionRule> AvgSalaryInclusionRules => Set<AvgSalaryInclusionRule>();
    public DbSet<EnforcementDeduction> EnforcementDeductions => Set<EnforcementDeduction>();
    public DbSet<NotebookRate> NotebookRates => Set<NotebookRate>();
    public DbSet<EmployeeBase> EmployeeBases => Set<EmployeeBase>();
    public DbSet<EmployeeAdmin> EmployeeAdmins => Set<EmployeeAdmin>();
    public DbSet<EmployeePkr> EmployeePkrs => Set<EmployeePkr>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<SystemParam> SystemParams => Set<SystemParam>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<WorkCalendar> WorkCalendars => Set<WorkCalendar>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Fluent API
        modelBuilder.Entity<EmployeeGpd>().HasKey(emp => emp.EmployeeId);
        modelBuilder.Entity<EmployeeNonPedagogical>().HasKey(emp => emp.EmployeeId);
        modelBuilder.Entity<SickLeave>().HasKey(emp => emp.Id);
        modelBuilder.Entity<Vacation>().HasKey(emp => emp.Id);
        modelBuilder.Entity<TrainingLeave>().HasKey(emp => emp.Id);
        modelBuilder.Entity<Calculation>().HasKey(emp => emp.Id);
        modelBuilder.Entity<EmployeeBase>().HasKey(emp => emp.EmployeeId);
        modelBuilder.Entity<EmployeeAdmin>().HasKey(emp => emp.EmployeeId);
        modelBuilder.Entity<EmployeePkr>().HasKey(emp => emp.EmployeeId);
        modelBuilder.Entity<Timesheet>().HasKey(emp => emp.Id);
        modelBuilder.Entity<SystemParam>().HasKey(emp => emp.Id);
        modelBuilder.Entity<WorkCalendar>().HasKey(emp => emp.Id);
        modelBuilder.Entity<EmployeeWorkload>().HasKey(x =>x.EmployeeId);
        modelBuilder.Entity<EmployeeAllowances>().HasKey(x =>x.EmployeeId);

        // Employee foreign keys
        modelBuilder.Entity<EmployeeWorkload>().HasOne(emp => emp.Employee).WithOne(emp => emp.Workload).HasForeignKey<EmployeeWorkload>(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmployeeAllowances>().HasOne(emp => emp.Employee).WithOne(emp => emp.Allowances).HasForeignKey<EmployeeAllowances>(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmployeeGpd>().HasOne(emp => emp.Employee).WithOne(emp => emp.Gpd).HasForeignKey<EmployeeGpd>(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Position>().HasOne(p => p.Department).WithMany().HasForeignKey(p => p.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Employee>().HasOne(e => e.Position).WithMany().HasForeignKey(e => e.PositionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Employee>().HasOne(e => e.TitleType).WithMany().HasForeignKey(e => e.TitleTypeId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<EmployeeNonPedagogical>().HasOne(emp => emp.Employee).WithOne(emp => emp.NonPedagogical).HasForeignKey<EmployeeNonPedagogical>(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmployeeBase>().HasOne(emp => emp.Employee).WithOne(emp => emp.Base).HasForeignKey<EmployeeBase>(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmployeeAdmin>().HasOne(emp => emp.Employee).WithOne(emp => emp.Admin).HasForeignKey<EmployeeAdmin>(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmployeePkr>().HasOne(emp => emp.Employee).WithOne(emp => emp.Pkr).HasForeignKey<EmployeePkr>(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SickLeave>().HasOne(emp => emp.Employee).WithMany().OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Vacation>().HasOne(emp => emp.Employee).WithMany().OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TrainingLeave>().HasOne(emp => emp.Employee).WithMany().OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Calculation>().HasOne(emp => emp.Employee).WithMany().OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CalculationPeriod>().HasOne(emp => emp.Calculation).WithMany(c => c.Periods).HasForeignKey(emp => emp.CalculationId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Employee>().HasIndex(e => e.TabNumber).IsUnique();
        modelBuilder.Entity<SystemParam>().HasIndex(e => e.Key).IsUnique();
        modelBuilder.Entity<TariffGrade>().HasIndex(e => e.Grade).IsUnique();
        modelBuilder.Entity<WorkCalendar>().HasIndex(e => new { e.Year, e.Month }).IsUnique();
        modelBuilder.Entity<Timesheet>().HasIndex(e => new { e.EmployeeId, e.Year, e.Month }).IsUnique();
        modelBuilder.Entity<Calculation>().HasIndex(e => new { e.EmployeeId, e.Year, e.Month }).IsUnique();
    }
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
    }
}
