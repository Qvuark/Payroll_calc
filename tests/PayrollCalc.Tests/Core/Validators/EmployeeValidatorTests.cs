using FluentAssertions;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Core.Validators;

namespace PayrollCalc.Tests.Core.Validators;

/// <summary>
/// Unit-тести чистих бізнес-правил EmployeeValidator (без БД).
/// ValidateBlocks — які блоки дозволені для WorkerClass. ValidateGradeForClass — діапазони розрядів.
/// </summary>
public class EmployeeValidatorTests
{
    // ---- ValidateBlocks ----

    [Fact]
    public void ValidateBlocks_Pedagogical_AllowsWorkloadAndAdmin()
    {
        // Class 1 (вчитель): навантаження + класне керівництво (Admin) — норма.
        var result = EmployeeValidator.ValidateBlocks(
            WorkerClass.Pedagogical,
            hasWorkload: true, hasAdmin: true, hasNonPedagogical: false, hasGpd: false, hasPkr: false);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateBlocks_Pedagogical_RejectsNonPedagogical()
    {
        var result = EmployeeValidator.ValidateBlocks(
            WorkerClass.Pedagogical,
            hasWorkload: false, hasAdmin: false, hasNonPedagogical: true, hasGpd: false, hasPkr: false);

        result.Should().ContainSingle()
            .Which.Should().Contain("непедагогічний");
    }

    [Fact]
    public void ValidateBlocks_AdminPedagogical_RejectsAdminBlock()
    {
        // C3: адмін-педагогічний (директор/завуч) не може мати Admin блок —
        // класне керівництво/кабінет лише в учителів.
        var result = EmployeeValidator.ValidateBlocks(
            WorkerClass.AdminPedagogical,
            hasWorkload: false, hasAdmin: true, hasNonPedagogical: false, hasGpd: false, hasPkr: false);

        result.Should().ContainSingle()
            .Which.Should().Contain("адміністративний блок");
    }

    [Fact]
    public void ValidateBlocks_AdminPedagogical_AllowsWorkload()
    {
        // Заступник що викладає — має право на навантаження (N-години).
        var result = EmployeeValidator.ValidateBlocks(
            WorkerClass.AdminPedagogical,
            hasWorkload: true, hasAdmin: false, hasNonPedagogical: false, hasGpd: false, hasPkr: false);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateBlocks_Specialist_RejectsWorkloadAdminGpdPkr()
    {
        var result = EmployeeValidator.ValidateBlocks(
            WorkerClass.Specialist,
            hasWorkload: true, hasAdmin: true, hasNonPedagogical: false, hasGpd: true, hasPkr: true);

        result.Should().HaveCount(4);
    }

    [Fact]
    public void ValidateBlocks_Specialist_AllowsNonPedagogical()
    {
        // Бухгалтер/бібліотекар — непедагогічний блок (наставництво/бібліотека) дозволений.
        var result = EmployeeValidator.ValidateBlocks(
            WorkerClass.Specialist,
            hasWorkload: false, hasAdmin: false, hasNonPedagogical: true, hasGpd: false, hasPkr: false);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateBlocks_Mop_RejectsPedagogicalBlocks()
    {
        var result = EmployeeValidator.ValidateBlocks(
            WorkerClass.MOP,
            hasWorkload: true, hasAdmin: true, hasNonPedagogical: false, hasGpd: true, hasPkr: true);

        result.Should().HaveCount(4);
    }

    [Fact]
    public void ValidateBlocks_Mop_AllowsNonPedagogical()
    {
        // МОП (прибиральник/сторож) — дезінфектанти/нічні живуть у непедагогічному блоці.
        var result = EmployeeValidator.ValidateBlocks(
            WorkerClass.MOP,
            hasWorkload: false, hasAdmin: false, hasNonPedagogical: true, hasGpd: false, hasPkr: false);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateBlocks_NoBlocks_ReturnsNull()
    {
        var result = EmployeeValidator.ValidateBlocks(
            WorkerClass.Pedagogical,
            hasWorkload: false, hasAdmin: false, hasNonPedagogical: false, hasGpd: false, hasPkr: false);

        result.Should().BeNull();
    }

    // ---- ValidateGradeForClass ----

    [Theory]
    [InlineData(WorkerClass.Pedagogical, 10, true)]
    [InlineData(WorkerClass.Pedagogical, 15, true)]
    [InlineData(WorkerClass.Pedagogical, 9, false)]
    [InlineData(WorkerClass.Pedagogical, 16, false)]
    [InlineData(WorkerClass.AdminPedagogical, 8, true)]
    [InlineData(WorkerClass.AdminPedagogical, 18, true)]
    [InlineData(WorkerClass.AdminPedagogical, 7, false)]
    [InlineData(WorkerClass.AdminPedagogical, 19, false)]
    [InlineData(WorkerClass.Specialist, 4, true)]
    [InlineData(WorkerClass.Specialist, 16, true)]
    [InlineData(WorkerClass.Specialist, 3, false)]
    [InlineData(WorkerClass.Specialist, 17, false)]
    [InlineData(WorkerClass.MOP, 1, true)]
    [InlineData(WorkerClass.MOP, 8, true)]
    [InlineData(WorkerClass.MOP, 0, false)]
    [InlineData(WorkerClass.MOP, 9, false)]
    public void ValidateGradeForClass_ChecksRangeBoundaries(WorkerClass workerClass, int grade, bool expected)
    {
        EmployeeValidator.ValidateGradeForClass(workerClass, grade).Should().Be(expected);
    }
}
