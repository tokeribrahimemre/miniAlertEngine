using MiniAlertEngine.Models;

namespace MiniAlertEngine.Rules;

/// <summary>
/// İçindeki tüm kurallar eşleşince eşleşir. İç kuralların sonuçları yalnızca
/// sinyal olarak kullanılır; bildirim yalnızca bu kök kuralın kimliğiyle üretilir.
/// </summary>
public class AndRule : IRule
{
    public string Id { get; }
    public IReadOnlyList<IRule> Rules { get; }

    public AndRule(string id, IEnumerable<IRule> rules)
    {
        var list = rules.ToList();
        if (list.Count == 0)
            throw new ArgumentException("'and' kuralı en az bir iç kural gerektirir.", nameof(rules));

        Id = id;
        Rules = list;
    }

    public Alert? Evaluate(PricePoint current, PricePoint? previous)
    {
        foreach (var rule in Rules)
        {
            if (rule.Evaluate(current, previous) is null)
                return null;
        }

        var message = $"Tüm koşullar sağlandı ({Rules.Count} kuralın tamamı eşleşti)";
        return new Alert(current.Time, Id, message, current.Price);
    }

    public Alert? Evaluate(PricePoint current, PricePoint? previous, EvaluationContext context)
    {
        foreach (var rule in Rules)
        {
            if (rule.Evaluate(current, previous, context) is null)
                return null;
        }

        var message = $"Tüm koşullar sağlandı ({Rules.Count} kuralın tamamı eşleşti)";
        return new Alert(current.Time, Id, message, current.Price);
    }
}
