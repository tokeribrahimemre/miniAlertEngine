using MiniAlertEngine;
using MiniAlertEngine.Models;
using MiniAlertEngine.Rules;
using Xunit;

namespace MiniAlertEngine.Tests;

public class StreakRuleTests
{
    private static readonly DateTimeOffset T0 = new(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static PricePoint At(int hourOffset, decimal price) =>
        new(T0.AddHours(hourOffset), price);

    [Fact]
    public void YukselisSerisi_HoursKadarGecisSonrasi_Eslesir()
    {
        // hours=3 → 4 gözlem (3 ardışık yukarı hareket) gerekir; 3. saatte eşleşir.
        var rule = new StreakRule("s1", 3, "up");
        var context = new EvaluationContext();

        Assert.Null(rule.Evaluate(At(0, 100m), null, context));
        Assert.Null(rule.Evaluate(At(1, 101m), At(0, 100m), context));
        Assert.Null(rule.Evaluate(At(2, 102m), At(1, 101m), context));

        var alert = rule.Evaluate(At(3, 103m), At(2, 102m), context);

        Assert.NotNull(alert);
        Assert.Equal("s1", alert.RuleId);
        Assert.Equal(103m, alert.Price);
    }

    [Fact]
    public void DususSerisi_HoursKadarGecisSonrasi_Eslesir()
    {
        var rule = new StreakRule("s2", 2, "down");
        var context = new EvaluationContext();

        rule.Evaluate(At(0, 100m), null, context);
        Assert.Null(rule.Evaluate(At(1, 99m), At(0, 100m), context));
        Assert.NotNull(rule.Evaluate(At(2, 98m), At(1, 99m), context));
    }

    [Fact]
    public void TersYonluSeri_Eslesmez()
    {
        var rule = new StreakRule("s3", 2, "up");
        var context = new EvaluationContext();

        rule.Evaluate(At(0, 100m), null, context);
        rule.Evaluate(At(1, 99m), At(0, 100m), context); // down

        Assert.Null(rule.Evaluate(At(2, 98m), At(1, 99m), context)); // 2 down ama kural up
    }

    [Fact]
    public void SabitFiyat_SeriyiKirar()
    {
        var rule = new StreakRule("s4", 2, "up");
        var context = new EvaluationContext();

        rule.Evaluate(At(0, 100m), null, context);
        rule.Evaluate(At(1, 101m), At(0, 100m), context); // up (1)
        rule.Evaluate(At(2, 101m), At(1, 101m), context); // sabit → seri kırıldı
        Assert.Null(rule.Evaluate(At(3, 102m), At(2, 101m), context)); // up (1) — 2'ye ulaşamadı
    }

    [Fact]
    public void YonDegisimi_SayaciSifirlar()
    {
        var rule = new StreakRule("s5", 3, "up");
        var context = new EvaluationContext();

        rule.Evaluate(At(0, 100m), null, context);
        rule.Evaluate(At(1, 101m), At(0, 100m), context); // up 1
        rule.Evaluate(At(2, 102m), At(1, 101m), context); // up 2
        rule.Evaluate(At(3, 101m), At(2, 102m), context); // down → sıfırla
        Assert.Null(rule.Evaluate(At(4, 102m), At(3, 101m), context)); // up 1
        Assert.Null(rule.Evaluate(At(5, 103m), At(4, 102m), context)); // up 2
        Assert.NotNull(rule.Evaluate(At(6, 104m), At(5, 103m), context)); // up 3 ✓
    }

    [Fact]
    public void EsikAsildiktanSonra_SeriSurdukce_EslesmeyeDevamEder()
    {
        var rule = new StreakRule("s6", 2, "up");
        var context = new EvaluationContext();

        rule.Evaluate(At(0, 100m), null, context);
        rule.Evaluate(At(1, 101m), At(0, 100m), context);
        Assert.NotNull(rule.Evaluate(At(2, 102m), At(1, 101m), context)); // 2 ✓
        Assert.NotNull(rule.Evaluate(At(3, 103m), At(2, 102m), context)); // 3 ✓ (sürdü)
    }

    [Fact]
    public void ContextsizCagri_HataVerir()
    {
        var rule = new StreakRule("s7", 2, "up");

        Assert.Throws<InvalidOperationException>(() => rule.Evaluate(At(0, 100m), null));
    }

    [Fact]
    public void GecersizParametreler_InsaSirasindaHataVerir()
    {
        Assert.Throws<ArgumentException>(() => new StreakRule("s8", 0, "up"));
        Assert.Throws<ArgumentException>(() => new StreakRule("s8", 2, "sideways"));
    }

    [Fact]
    public void Motor_StreakKuraliniSaatSaatBesler()
    {
        // Fiyatlar: 100, 101, 102, 103 → 3 ardışık yukarı geçiş, 3. saatte eşleşme.
        var engine = new AlertEngine(new IRule[] { new StreakRule("s9", 3, "up") });
        var prices = new[] { At(0, 100m), At(1, 101m), At(2, 102m), At(3, 103m) };

        var alerts = engine.Run(prices).ToList();

        Assert.Single(alerts);
        Assert.Equal(T0.AddHours(3), alerts[0].Time);
    }

    [Fact]
    public void Motor_HerCalistirmadaStateSifirlanir()
    {
        var engine = new AlertEngine(new IRule[] { new StreakRule("s10", 2, "up") });
        var prices = new[] { At(0, 100m), At(1, 101m) };

        // İlk çalıştırma: 1 geçiş var, hours=2'ye ulaşılmaz.
        Assert.Empty(engine.Run(prices));
        // İkinci çalıştırmada state taşsaydı sayaç 2 olurdu; sıfırlandığı için yine boş.
        Assert.Empty(engine.Run(prices));
    }

    [Fact]
    public void IkiStreakKurali_StateBirbiriniKirletmez()
    {
        // Aynı context'i paylaşan iki kural, kendi id'leriyle izole state tutar.
        var upRule = new StreakRule("up2", 2, "up");
        var downRule = new StreakRule("down2", 2, "down");
        var context = new EvaluationContext();

        // Fiyatlar: 100, 101, 102 → up kuralı 2. saatte eşleşir; down kuralı hiç eşleşmez.
        upRule.Evaluate(At(0, 100m), null, context);
        downRule.Evaluate(At(0, 100m), null, context);
        upRule.Evaluate(At(1, 101m), At(0, 100m), context);
        downRule.Evaluate(At(1, 101m), At(0, 100m), context);

        Assert.NotNull(upRule.Evaluate(At(2, 102m), At(1, 101m), context));
        Assert.Null(downRule.Evaluate(At(2, 102m), At(1, 101m), context));
    }

    [Fact]
    public void BilesikKuralIcerisinde_Calisir()
    {
        // and(threshold gt 100, streak up 2): iki koşul birlikte sağlanınca eşleşir.
        var rule = new AndRule("combo", new IRule[]
        {
            new ThresholdRule("<anon>", "gt", 100m),
            new StreakRule("<anon>", 2, "up")
        });
        var context = new EvaluationContext();

        Assert.Null(rule.Evaluate(At(0, 100m), null, context)); // eşik sağlanmadı
        Assert.Null(rule.Evaluate(At(1, 101m), At(0, 100m), context)); // streak 1
        Assert.NotNull(rule.Evaluate(At(2, 102m), At(1, 101m), context)); // streak 2 + eşik ✓
    }
}
