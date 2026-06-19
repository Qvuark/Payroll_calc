using FluentAssertions;
using PayrollCalc.API.Application.Import;
using PayrollCalc.Core.Interfaces;

namespace PayrollCalc.Tests.Application.Import;

public class AliasMatcherTests
{
    // Легкий двійник запису довідника — Match працює з будь-яким IAliasable, реальна БД не потрібна.
    private sealed record FakeAliasable(string Name, List<string> ExcelAliases) : IAliasable;

    [Theory]
    [InlineData("Вчитель", "вчитель")]
    [InlineData("  Вчитель  ", "вчитель")]
    [InlineData("ВЧИТЕЛЬ", "вчитель")]
    [InlineData("вч.математики", "вч математики")]
    [InlineData("вч.  математики", "вч математики")]
    [InlineData("Вч. Математики", "вч математики")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void Normalize_ReducesToCanonicalForm(string? input, string expected)
    {
        AliasMatcher.Normalize(input).Should().Be(expected);
    }

    [Fact]
    public void Match_ByExactName_ReturnsRecord()
    {
        var items = new[] { new FakeAliasable("Вчитель", []) };

        var result = AliasMatcher.Match(items, "Вчитель");

        result.Should().ContainSingle().Which.Name.Should().Be("Вчитель");
    }

    [Fact]
    public void Match_ByAlias_ReturnsRecord()
    {
        var items = new[] { new FakeAliasable("Вчитель", ["вч.", "вч.математики"]) };

        var result = AliasMatcher.Match(items, "вч.");

        result.Should().ContainSingle().Which.Name.Should().Be("Вчитель");
    }

    [Fact]
    public void Match_IgnoresCaseDotsSpaces()
    {
        var items = new[] { new FakeAliasable("Вчитель", ["вч.математики"]) };

        var result = AliasMatcher.Match(items, "ВЧ.  Математики");

        result.Should().ContainSingle();
    }

    [Fact]
    public void Match_NoMatch_ReturnsEmpty()
    {
        var items = new[] { new FakeAliasable("Вчитель", ["вч."]) };

        var result = AliasMatcher.Match(items, "Прибиральник");

        result.Should().BeEmpty();
    }

    [Fact]
    public void Match_Ambiguous_ReturnsAllMatches()
    {
        // Дві записи претендують на той самий рядок (name однієї = alias іншої) → викликач має побачити 2.
        var items = new[]
        {
            new FakeAliasable("Вчитель", []),
            new FakeAliasable("Інша посада", ["вчитель"]),
        };

        var result = AliasMatcher.Match(items, "Вчитель");

        result.Should().HaveCount(2);
    }
}
