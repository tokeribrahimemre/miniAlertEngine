using MiniAlertEngine.Models;
using MiniAlertEngine.Rules;
using Xunit;

namespace MiniAlertEngine.Tests;

public class AlertEngineTests
{
    private static readonly DateTimeOffset T0 = new(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static PricePoint At(int hourOffset, decimal price) =>
        new(T0.AddHours(hourOffset), price);

    [Fact]
    public void Motor_FiyatlariSiraylaGezer_VeOncekiFiyatiKuralaIletir()
    {
        var rules = new IRule[]
        {
            new ThresholdRule("high", "gt", 100m),
            new ChangeRule("jump", 10m)
        };
        var engine = new AlertEngine(rules);

        var prices = new[] { At(0, 100m), At(1, 120m), At(2, 121m) };
        var alerts = engine.Run(prices).ToList();

        // Saat 1: hem "high" (120 > 100) hem "jump" (%20 >= %10) eşleşir.
        Assert.Equal(2, alerts.Count(a => a.Time == T0.AddHours(1)));
        // Saat 2: "high" eşleşir, "jump" eşleşmez (%0,83 < %10).
        var hour2 = alerts.Where(a => a.Time == T0.AddHours(2)).ToList();
        Assert.Single(hour2);
        Assert.Equal("high", hour2[0].RuleId);
        // İlk saat: "jump" asla eşleşmez.
        Assert.DoesNotContain(alerts, a => a.Time == T0 && a.RuleId == "jump");
    }

    [Fact]
    public void Motor_EslesmeYoksa_BosDoner()
    {
        var engine = new AlertEngine(new IRule[] { new ThresholdRule("high", "gt", 1000m) });

        var alerts = engine.Run(new[] { At(0, 1m), At(1, 2m) });

        Assert.Empty(alerts);
    }

    [Fact]
    public void RuleFactory_TumTipleriOlusturur()
    {
        var threshold = RuleFactory.Create(new RuleDefinition { Id = "a", Type = "threshold", Operator = "gt", Value = 5m });
        var change = RuleFactory.Create(new RuleDefinition { Id = "b", Type = "change", Percent = 5m });
        var range = RuleFactory.Create(new RuleDefinition { Id = "c", Type = "range", Min = 1m, Max = 9m });

        Assert.IsType<ThresholdRule>(threshold);
        Assert.IsType<ChangeRule>(change);
        Assert.IsType<RangeRule>(range);
    }

    [Fact]
    public void RuleFactory_BilinmeyenTip_HataVerir()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RuleFactory.Create(new RuleDefinition { Id = "x", Type = "magic" }));
    }
}
