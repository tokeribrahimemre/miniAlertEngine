using MiniAlertEngine.Models;
using MiniAlertEngine.Rules;
using Xunit;

namespace MiniAlertEngine.Tests;

public class RangeRuleTests
{
    private static readonly DateTimeOffset T = new(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FiyatBandinIcinde_Eslesmez()
    {
        var rule = new RangeRule("r1", 90m, 110m);

        Assert.Null(rule.Evaluate(new PricePoint(T, 100m), previous: null));
    }

    [Theory]
    [InlineData(90)]
    [InlineData(110)]
    public void FiyatSinirda_Eslesmez(double price)
    {
        var rule = new RangeRule("r1", 90m, 110m);

        Assert.Null(rule.Evaluate(new PricePoint(T, (decimal)price), previous: null));
    }

    [Fact]
    public void FiyatAltSinirinAltinda_Eslesir()
    {
        var rule = new RangeRule("r1", 90m, 110m);

        var alert = rule.Evaluate(new PricePoint(T, 89m), previous: null);

        Assert.NotNull(alert);
        Assert.Equal("r1", alert.RuleId);
    }

    [Fact]
    public void FiyatUstSinirinUstunde_Eslesir()
    {
        var rule = new RangeRule("r1", 90m, 110m);

        Assert.NotNull(rule.Evaluate(new PricePoint(T, 111m), previous: null));
    }

    [Fact]
    public void MinMaxtanBuyuk_InsaSirasindaHataVerir()
    {
        Assert.Throws<ArgumentException>(() => new RangeRule("r2", 110m, 90m));
    }
}
