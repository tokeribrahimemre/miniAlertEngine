using MiniAlertEngine.Models;

namespace MiniAlertEngine.Rules;

/// <summary>
/// İçindeki kurallardan en az biri eşleşince eşleşir. İç kuralların sonuçları
/// yalnızca sinyal olarak kullanılır; bildirim yalnızca bu kök kuralın kimliğiyle üretilir.
/// </summary>
public class OrRule : IRule
{
    public string Id { get; }
    public IReadOnlyList<IRule> Rules { get; }

    public OrRule(string id, IEnumerable<IRule> rules)
    {
        var list = rules.ToList();
        if (list.Count == 0)
            throw new ArgumentException("'or' kuralı en az bir iç kural gerektirir.", nameof(rules));

        Id = id;
        Rules = list;
    }

    public Alert? Evaluate(PricePoint current, PricePoint? previous)
    {
        foreach (var rule in Rules)
        {
            if (rule.Evaluate(current, previous) is not null)
            {
                var message = "Koşullardan en az biri sağlandı";
                return new Alert(current.Time, Id, message, current.Price);
            }
        }

        return null;
    }

    public Alert? Evaluate(PricePoint current, PricePoint? previous, EvaluationContext context)
    {
        foreach (var rule in Rules)
        {
            if (rule.Evaluate(current, previous, context) is not null)
            {
                var message = "Koşullardan en az biri sağlandı";
                return new Alert(current.Time, Id, message, current.Price);
            }
        }

        return null;
    }
}
