using MiniAlertEngine.Models;
using MiniAlertEngine.Rules;
using Xunit;

namespace MiniAlertEngine.Tests;

public class ChangeRuleTests
{
    private static readonly DateTimeOffset T0 = new(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddHours(1);

    [Fact]
    public void IlkSaat_OncekiFiyatYok_Eslesmez()
    {
        // Karar: ilk saatte karşılaştırma tabanı olmadığından kural sessiz kalır.
        var rule = new ChangeRule("c1", 5m);

        var alert = rule.Evaluate(new PricePoint(T0, 100m), previous: null);

        Assert.Null(alert);
    }

    [Fact]
    public void YuzdeArtisEsigiAsinca_Eslesir()
    {
        var rule = new ChangeRule("c1", 5m);
        var previous = new PricePoint(T0, 100m);

        var alert = rule.Evaluate(new PricePoint(T1, 106m), previous);

        Assert.NotNull(alert);
        Assert.Equal("c1", alert.RuleId);
        Assert.Equal(106m, alert.Price);
    }

    [Fact]
    public void YuzdeDususEsigiAsinca_Eslesir()
    {
        var rule = new ChangeRule("c1", 5m);
        var previous = new PricePoint(T0, 100m);

        var alert = rule.Evaluate(new PricePoint(T1, 94m), previous);

        Assert.NotNull(alert);
        Assert.Contains("düşüş", alert.Message);
    }

    [Fact]
    public void DegisimEsikAltinda_Eslesmez()
    {
        var rule = new ChangeRule("c1", 5m);
        var previous = new PricePoint(T0, 100m);

        Assert.Null(rule.Evaluate(new PricePoint(T1, 103m), previous));
    }

    [Fact]
    public void DegisimTamEsikte_Eslesir()
    {
        var rule = new ChangeRule("c1", 5m);
        var previous = new PricePoint(T0, 100m);

        Assert.NotNull(rule.Evaluate(new PricePoint(T1, 105m), previous));
    }

    [Fact]
    public void NegatifYuzde_InsaSirasindaHataVerir()
    {
        Assert.Throws<ArgumentException>(() => new ChangeRule("c2", -1m));
    }
}
