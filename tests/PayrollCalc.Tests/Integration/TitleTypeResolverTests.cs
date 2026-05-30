using FluentAssertions;
using PayrollCalc.API.Application.Import;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Documents.Import.Common;

namespace PayrollCalc.Tests.Integration;

/// <summary>
/// Integration-тести TitleTypeResolver проти реального Postgres (резолв читає довідник TitleTypes).
/// Перевіряємо scope per WorkerClass, тихий null на порожньому вводі, ParserError на невідомому званні.
/// </summary>
public class TitleTypeResolverTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;
    public TitleTypeResolverTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Resolve_KnownTitleInScope_ReturnsId()
    {
        await using var db = _fixture.CreateContext();
        var errors = new List<ParserError>();

        var id = await TitleTypeResolver.ResolveTitleTypeIdAsync(
            db, "Старший вчитель", WorkerClass.Pedagogical, rowIndex: 1, errors);

        id.Should().NotBeNull();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Resolve_KnownTitleWrongScope_ReturnsNullWithError()
    {
        // "Старший вчитель" існує лише для Pedagogical. Запит у scope AdminPedagogical → не знайдено.
        await using var db = _fixture.CreateContext();
        var errors = new List<ParserError>();

        var id = await TitleTypeResolver.ResolveTitleTypeIdAsync(
            db, "Старший вчитель", WorkerClass.AdminPedagogical, rowIndex: 1, errors);

        id.Should().BeNull();
        errors.Should().ContainSingle().Which.Field.Should().Be("TitleType");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Resolve_EmptyName_ReturnsNullNoError(string? name)
    {
        // Порожнє звання — норма (працівник без звання), не помилка.
        await using var db = _fixture.CreateContext();
        var errors = new List<ParserError>();

        var id = await TitleTypeResolver.ResolveTitleTypeIdAsync(
            db, name, WorkerClass.Pedagogical, rowIndex: 1, errors);

        id.Should().BeNull();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Resolve_UnknownTitle_ReturnsNullWithError()
    {
        await using var db = _fixture.CreateContext();
        var errors = new List<ParserError>();

        var id = await TitleTypeResolver.ResolveTitleTypeIdAsync(
            db, "Неіснуюче звання", WorkerClass.Pedagogical, rowIndex: 7, errors);

        id.Should().BeNull();
        errors.Should().ContainSingle().Which.Row.Should().Be(7);
    }
}
