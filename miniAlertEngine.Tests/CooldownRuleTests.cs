using MiniAlertEngine;
using MiniAlertEngine.Models;
using MiniAlertEngine.Rules;
using Xunit;

namespace MiniAlertEngine.Tests;

public class CooldownRuleTests
{
    private static readonly DateTimeOffset T0 = new(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static PricePoint At(int hourOffset, decimal price) =>
        new(T0.AddHours(hourOffset), price);

    [Fact]
    public void IlkEslesme_HerZamanBasilir()
    {
        var rule = new CooldownRule("c1", 6, new ThresholdRule("<anon>", "gt", 100m));
        var context = new EvaluationContext();

        var alert = rule.Evaluate(At(0, 150m), null, context);

        Assert.NotNull(alert);
        Assert.Equal("c1", alert.RuleId);
        Assert.Equal(150m, alert.Price);
    }

    [Fact]
    public void PencereIcindekiEslesmeler_Yutulur()
    {
        // hours=3 → son bildirimden 3 saat geçmeden tekrar basılmaz.
        var rule = new CooldownRule("c2", 3, new ThresholdRule("<anon>", "gt", 100m));
        var context = new EvaluationContext();

        Assert.NotNull(rule.Evaluate(At(0, 150m), null, context)); // basılır
        Assert.Null(rule.Evaluate(At(1, 150m), At(0, 150m), context)); // +1 saat < 3 → yutulur
        Assert.Null(rule.Evaluate(At(2, 150m), At(1, 150m), context)); // +2 saat < 3 → yutulur
    }

    [Fact]
    public void PencereDolunca_TekrarBasilir()
    {
        var rule = new CooldownRule("c3", 3, new ThresholdRule("<anon>", "gt", 100m));
        var context = new EvaluationContext();

        Assert.NotNull(rule.Evaluate(At(0, 150m), null, context));
        Assert.Null(rule.Evaluate(At(1, 150m), At(0, 150m), context));
        Assert.Null(rule.Evaluate(At(2, 150m), At(1, 150m), context));
        Assert.NotNull(rule.Evaluate(At(3, 150m), At(2, 150m), context)); // +3 saat → basılır
    }

    [Fact]
    public void YutulanEslesmeler_SayaciBaslatmaz()
    {
        // Pencere, YUTULAN eşleşmelerle değil sadece BASILAN bildirimle ilerler.
        var rule = new CooldownRule("c4", 2, new ThresholdRule("<anon>", "gt", 100m));
        var context = new EvaluationContext();

        Assert.NotNull(rule.Evaluate(At(0, 150m), null, context)); // t=0 basıldı
        Assert.Null(rule.Evaluate(At(1, 150m), At(0, 150m), context)); // yutuldu, sayaç güncellenmez
        Assert.NotNull(rule.Evaluate(At(2, 150m), At(1, 150m), context)); // t=0'dan 2 saat → basılır
    }

    [Fact]
    public void IcKuralEslesmezse_SayacEtkilenmez()
    {
        var rule = new CooldownRule("c5", 2, new ThresholdRule("<anon>", "gt", 100m));
        var context = new EvaluationContext();

        Assert.NotNull(rule.Evaluate(At(0, 150m), null, context)); // basıldı (t=0)
        Assert.Null(rule.Evaluate(At(1, 50m), At(0, 150m), context)); // iç kural eşleşmedi
        Assert.NotNull(rule.Evaluate(At(2, 150m), At(1, 50m), context)); // t=0'dan 2 saat → basılır
    }

    [Fact]
    public void BilesikKuralIcerisinde_Calisir()
    {
        // cooldown(and(threshold gt 100, change 5%)): birleşim sinyalini sınırlar.
        var inner = new AndRule("<anon>", new IRule[]
        {
            new ThresholdRule("<anon>", "gt", 100m),
            new ChangeRule("<anon>", 5m)
        });
        var rule = new CooldownRule("c6", 3, inner);
        var context = new EvaluationContext();

        Assert.Null(rule.Evaluate(At(0, 100m), null, context)); // change ilk saat eşleşmez
        Assert.NotNull(rule.Evaluate(At(1, 120m), At(0, 100m), context)); // ikisi de ✓ → basılır
        Assert.Null(rule.Evaluate(At(2, 150m), At(1, 120m), context)); // iç kural ✓ ama pencerede → yutulur
    }

    [Fact]
    public void ContextsizCagri_HataVerir()
    {
        var rule = new CooldownRule("c7", 2, new ThresholdRule("<anon>", "gt", 100m));

        Assert.Throws<InvalidOperationException>(() => rule.Evaluate(At(0, 150m), null));
    }

    [Fact]
    public void GecersizParametreler_InsaSirasindaHataVerir()
    {
        Assert.Throws<ArgumentException>(() => new CooldownRule("c8", 0, new ThresholdRule("<anon>", "gt", 1m)));
        Assert.Throws<ArgumentNullException>(() => new CooldownRule("c8", 2, null!));
    }

    [Fact]
    public void Motor_CooldownKuraliniSaatSaatBesler()
    {
        // 5 saat boyunca sürekli 150 (eşik 100): cooldown=2 ile t=0, t=2, t=4 basılır.
        var engine = new AlertEngine(new IRule[]
        {
            new CooldownRule("c9", 2, new ThresholdRule("<anon>", "gt", 100m))
        });
        var prices = Enumerable.Range(0, 5).Select(h => At(h, 150m)).ToArray();

        var alerts = engine.Run(prices).ToList();

        Assert.Equal(3, alerts.Count);
        Assert.Equal(new[] { T0, T0.AddHours(2), T0.AddHours(4) }, alerts.Select(a => a.Time).ToArray());
        Assert.All(alerts, a => Assert.Equal("c9", a.RuleId));
    }

    [Fact]
    public void Factory_StreakVeCooldown_Uretir()
    {
        var streak = RuleFactory.Create(new RuleDefinition
        {
            Id = "st",
            Type = "streak",
            Hours = 3,
            Direction = "up"
        });
        var cooldown = RuleFactory.Create(new RuleDefinition
        {
            Id = "cd",
            Type = "cooldown",
            Hours = 6,
            Rule = new RuleDefinition { Type = "threshold", Operator = "gt", Value = 100m }
        });

        Assert.IsType<StreakRule>(streak);
        var cd = Assert.IsType<CooldownRule>(cooldown);
        Assert.IsType<ThresholdRule>(cd.Rule);
    }

    [Fact]
    public void DurumBilenIcKurali_Sarabilir()
    {
        // cooldown(streak up 1): seri her saat eşleşse de bildirim hours=2 ile sınırlanır.
        var rule = new CooldownRule("c10", 2, new StreakRule("<anon>", 1, "up"));
        var context = new EvaluationContext();

        Assert.Null(rule.Evaluate(At(0, 100m), null, context)); // henüz hareket yok
        Assert.NotNull(rule.Evaluate(At(1, 101m), At(0, 100m), context)); // streak ✓ → basılır (t=1)
        Assert.Null(rule.Evaluate(At(2, 102m), At(1, 101m), context)); // streak ✓ ama pencerede
        Assert.NotNull(rule.Evaluate(At(3, 103m), At(2, 102m), context)); // pencere doldu → basılır
    }

    [Fact]
    public void Factory_EksikAlanlarda_HataVerir()
    {
        Assert.Throws<InvalidOperationException>(() => RuleFactory.Create(new RuleDefinition
        {
            Id = "bad-streak",
            Type = "streak",
            Hours = 3 // direction eksik
        }));

        Assert.Throws<InvalidOperationException>(() => RuleFactory.Create(new RuleDefinition
        {
            Id = "bad-cooldown",
            Type = "cooldown",
            Hours = 2 // rule eksik
        }));
    }
}
