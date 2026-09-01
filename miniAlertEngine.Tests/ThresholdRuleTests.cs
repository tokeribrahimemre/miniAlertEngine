using MiniAlertEngine.Models;
using MiniAlertEngine.Rules;
using Xunit;

namespace MiniAlertEngine.Tests;

public class ThresholdRuleTests
{
    private static readonly DateTimeOffset T = new(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Gt_FiyatEsikUstunde_Eslesir()
    {
        var rule = new ThresholdRule("t1", "gt", 100m);

        var alert = rule.Evaluate(new PricePoint(T, 101m), previous: null);

        Assert.NotNull(alert);
        Assert.Equal("t1", alert.RuleId);
        Assert.Equal(101m, alert.Price);
    }

    [Fact]
    public void Gt_FiyatEsigeEsit_Eslesmez()
    {
        var rule = new ThresholdRule("t1", "gt", 100m);

        var alert = rule.Evaluate(new PricePoint(T, 100m), previous: null);

        Assert.Null(alert);
    }

    [Fact]
    public void Gt_FiyatEsikAltinda_Eslesmez()
    {
        var rule = new ThresholdRule("t1", "gt", 100m);

        Assert.Null(rule.Evaluate(new PricePoint(T, 99m), previous: null));
    }

    [Fact]
    public void Lt_FiyatEsikAltinda_Eslesir()
    {
        var rule = new ThresholdRule("t2", "lt", 50m);

        var alert = rule.Evaluate(new PricePoint(T, 49m), previous: null);

        Assert.NotNull(alert);
    }

    [Fact]
    public void Lt_FiyatEsikUstunde_Eslesmez()
    {
        var rule = new ThresholdRule("t2", "lt", 50m);

        Assert.Null(rule.Evaluate(new PricePoint(T, 51m), previous: null));
    }

    [Fact]
    public void GecersizOperator_InsaSirasindaHataVerir()
    {
        Assert.Throws<ArgumentException>(() => new ThresholdRule("t3", "eq", 100m));
    }
}
