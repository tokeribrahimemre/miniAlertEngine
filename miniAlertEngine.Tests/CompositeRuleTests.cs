using MiniAlertEngine.Models;
using MiniAlertEngine.Rules;
using Xunit;

namespace MiniAlertEngine.Tests;

public class CompositeRuleTests
{
    private static readonly DateTimeOffset T0 = new(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddHours(1);

    // ---------- AndRule ----------

    [Fact]
    public void And_TumKurallarEslesince_Eslesir()
    {
        var rule = new AndRule("a1", new IRule[]
        {
            new ThresholdRule("<anon>", "gt", 100m),
            new RangeRule("<anon>", 90m, 105m)
        });

        var alert = rule.Evaluate(new PricePoint(T0, 110m), previous: null);

        Assert.NotNull(alert);
        Assert.Equal("a1", alert.RuleId);
        Assert.Equal(110m, alert.Price);
    }

    [Fact]
    public void And_BirKuralEslesmezse_Eslesmez()
    {
        var rule = new AndRule("a1", new IRule[]
        {
            new ThresholdRule("<anon>", "gt", 100m),
            new RangeRule("<anon>", 100m, 120m)
        });

        Assert.Null(rule.Evaluate(new PricePoint(T0, 110m), previous: null));
    }

    [Fact]
    public void And_BosKuralListesi_HataVerir()
    {
        Assert.Throws<ArgumentException>(() => new AndRule("a2", Array.Empty<IRule>()));
    }

    // ---------- OrRule ----------

    [Fact]
    public void Or_EnAzBirKuralEslesince_Eslesir()
    {
        var rule = new OrRule("o1", new IRule[]
        {
            new ThresholdRule("<anon>", "gt", 100m),
            new ThresholdRule("<anon>", "lt", 50m)
        });

        Assert.NotNull(rule.Evaluate(new PricePoint(T0, 110m), previous: null));
        Assert.NotNull(rule.Evaluate(new PricePoint(T0, 40m), previous: null));
    }

    [Fact]
    public void Or_HicbirKuralEslesmezse_Eslesmez()
    {
        var rule = new OrRule("o1", new IRule[]
        {
            new ThresholdRule("<anon>", "gt", 100m),
            new ThresholdRule("<anon>", "lt", 50m)
        });

        Assert.Null(rule.Evaluate(new PricePoint(T0, 75m), previous: null));
    }

    [Fact]
    public void Or_BosKuralListesi_HataVerir()
    {
        Assert.Throws<ArgumentException>(() => new OrRule("o2", Array.Empty<IRule>()));
    }

    // ---------- NotRule ----------

    [Fact]
    public void Not_IcKuralEslesmezse_Eslesir()
    {
        var rule = new NotRule("n1", new ThresholdRule("<anon>", "gt", 100m));

        var alert = rule.Evaluate(new PricePoint(T0, 90m), previous: null);

        Assert.NotNull(alert);
        Assert.Equal("n1", alert.RuleId);
    }

    [Fact]
    public void Not_IcKuralEslesince_Eslesmez()
    {
        var rule = new NotRule("n1", new ThresholdRule("<anon>", "gt", 100m));

        Assert.Null(rule.Evaluate(new PricePoint(T0, 110m), previous: null));
    }

    [Fact]
    public void Not_IlkSaatteChangeIcKuraliyla_Eslesir()
    {
        // change ilk saatte hiçbir zaman eşleşmediğinden, not(change) ilk saatte eşleşir.
        var rule = new NotRule("n2", new ChangeRule("<anon>", 5m));

        Assert.NotNull(rule.Evaluate(new PricePoint(T0, 100m), previous: null));
    }

    // ---------- İç içe geçme ----------

    [Fact]
    public void IcIceGecme_AndIcindeOrIcindeNot_DogruCalisir()
    {
        // and( threshold gt 100, or( not(range 90-120), threshold lt 80 ) )
        var rule = new AndRule("root", new IRule[]
        {
            new ThresholdRule("<anon>", "gt", 100m),
            new OrRule("<anon>", new IRule[]
            {
                new NotRule("<anon>", new RangeRule("<anon>", 90m, 120m)),
                new ThresholdRule("<anon>", "lt", 80m)
            })
        });

        // Fiyat 150: gt 100 ✓; or için: not(range) → 150 bant dışı, range eşleşir → not eşleşmez; lt 80 ✗ → or eşleşmez.
        Assert.Null(rule.Evaluate(new PricePoint(T0, 150m), previous: null));

        // Fiyat 110: gt 100 ✓; or için: not(range) → 110 bant içi, range eşleşmez → not eşleşir ✓ → or eşleşir ✓.
        Assert.NotNull(rule.Evaluate(new PricePoint(T0, 110m), previous: null));
    }

    [Fact]
    public void IcIceGecme_ChangeKuraliOncekiFiyatiBirlesimIcindeDeAlir()
    {
        var rule = new AndRule("root", new IRule[]
        {
            new ThresholdRule("<anon>", "gt", 100m),
            new ChangeRule("<anon>", 5m)
        });
        var previous = new PricePoint(T0, 100m);

        // 120: gt 100 ✓, change %20 ✓ → eşleşir.
        Assert.NotNull(rule.Evaluate(new PricePoint(T1, 120m), previous));
        // 103: gt 100 ✓, change %3 ✗ → eşleşmez.
        Assert.Null(rule.Evaluate(new PricePoint(T1, 103m), previous));
    }

    // ---------- Factory (recursive üretim) ----------

    [Fact]
    public void Factory_AndIcindeOrIcindeNot_Uretir()
    {
        var definition = new RuleDefinition
        {
            Id = "root",
            Type = "and",
            Rules = new List<RuleDefinition>
            {
                new() { Type = "threshold", Operator = "gt", Value = 100m },
                new()
                {
                    Type = "or",
                    Rules = new List<RuleDefinition>
                    {
                        new()
                        {
                            Type = "not",
                            Rule = new RuleDefinition { Type = "range", Min = 90m, Max = 120m }
                        },
                        new() { Type = "threshold", Operator = "lt", Value = 80m }
                    }
                }
            }
        };

        var rule = RuleFactory.Create(definition);

        var and = Assert.IsType<AndRule>(rule);
        Assert.Equal("root", and.Id);
        Assert.Equal(2, and.Rules.Count);
        var or = Assert.IsType<OrRule>(and.Rules[1]);
        Assert.IsType<NotRule>(or.Rules[0]);
    }

    [Fact]
    public void Factory_IcKurallarinIdsizTanimlanmasinaIzinVerir()
    {
        var definition = new RuleDefinition
        {
            Id = "root",
            Type = "and",
            Rules = new List<RuleDefinition>
            {
                new() { Type = "threshold", Operator = "gt", Value = 100m },
                new() { Type = "change", Percent = 5m }
            }
        };

        var rule = RuleFactory.Create(definition);

        Assert.IsType<AndRule>(rule);
    }

    [Fact]
    public void Factory_KokKuraldaIdEksikse_HataVerir()
    {
        var definition = new RuleDefinition
        {
            Type = "threshold",
            Operator = "gt",
            Value = 100m
        };

        Assert.Throws<InvalidOperationException>(() => RuleFactory.Create(definition));
    }

    [Fact]
    public void Factory_NotKuralindaRuleEksikse_HataVerir()
    {
        var definition = new RuleDefinition { Id = "n", Type = "not" };

        Assert.Throws<InvalidOperationException>(() => RuleFactory.Create(definition));
    }

    // ---------- Motor: iç kurallar bildirim basmaz ----------

    [Fact]
    public void Motor_IcKurallarAslaKendiBasinaBildirimBasmaz()
    {
        var engine = new AlertEngine(new IRule[]
        {
            new AndRule("root", new IRule[]
            {
                new ThresholdRule("<anon>", "gt", 100m),
                new ChangeRule("<anon>", 5m)
            })
        });

        var prices = new[]
        {
            new PricePoint(T0, 100m),
            new PricePoint(T1, 120m)
        };

        var alerts = engine.Run(prices).ToList();

        // Saat 1'de hem threshold hem change eşleşir ama tek bildirim basılır (kök kural).
        var hour1 = alerts.Where(a => a.Time == T1).ToList();
        Assert.Single(hour1);
        Assert.Equal("root", hour1[0].RuleId);
    }
}
