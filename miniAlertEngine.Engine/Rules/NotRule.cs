using MiniAlertEngine.Models;

namespace MiniAlertEngine.Rules;

/// <summary>
/// Taşıdığı tek iç kural eşleşmediğinde eşleşir. İç kuralın sonucu yalnızca
/// sinyal olarak kullanılır; bildirim yalnızca bu kök kuralın kimliğiyle üretilir.
/// </summary>
public class NotRule : IRule
{
    public string Id { get; }
    public IRule Rule { get; }

    public NotRule(string id, IRule rule)
    {
        Id = id;
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
    }

    public Alert? Evaluate(PricePoint current, PricePoint? previous)
    {
        if (Rule.Evaluate(current, previous) is not null)
            return null;

        var message = "İç kural sağlanmadı (koşulun tersi geçerli)";
        return new Alert(current.Time, Id, message, current.Price);
    }
}
