using MiniAlertEngine.Models;

namespace MiniAlertEngine.Rules;

/// <summary>
/// Başka bir kuralı sarar ve bildirim sıklığını sınırlar: ilk eşleşme her zaman
/// basılır; sonrasında, son bildirimden itibaren 'hours' saat dolmadan tekrar
/// bildirim basılmaz (iç kural bu arada eşleşmeye devam etse bile).
/// Durum, saatler arası <see cref="EvaluationContext"/> içinde tutulur.
/// </summary>
public class CooldownRule : IRule
{
    public string Id { get; }
    public int Hours { get; }
    public IRule Rule { get; }

    public CooldownRule(string id, int hours, IRule rule)
    {
        if (hours < 1)
            throw new ArgumentException("'hours' en az 1 olmalı.", nameof(hours));

        Id = id;
        Hours = hours;
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
    }

    public Alert? Evaluate(PricePoint current, PricePoint? previous) =>
        throw new InvalidOperationException(
            "CooldownRule durum bilgisi gerektirir; context'li Evaluate üzerinden çağrılmalıdır.");

    public Alert? Evaluate(PricePoint current, PricePoint? previous, EvaluationContext context)
    {
        var inner = Rule.Evaluate(current, previous, context);
        if (inner is null)
            return null;

        var state = context.GetState(Id, () => new CooldownState());

        if (state.LastAlertTime is not null &&
            current.Time - state.LastAlertTime.Value < TimeSpan.FromHours(Hours))
        {
            return null;
        }

        state.LastAlertTime = current.Time;
        var message = $"{inner.Message} (cooldown: {Hours} saat)";
        return new Alert(current.Time, Id, message, current.Price);
    }

    private class CooldownState
    {
        public DateTimeOffset? LastAlertTime { get; set; }
    }
}
