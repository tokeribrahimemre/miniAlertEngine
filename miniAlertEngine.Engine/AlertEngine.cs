using MiniAlertEngine.Models;
using MiniAlertEngine.Rules;

namespace MiniAlertEngine;

/// <summary>
/// Fiyat serisini saat saat gezer ve her adımda tüm kuralları değerlendirir.
/// </summary>
public class AlertEngine
{
    private readonly IReadOnlyList<IRule> _rules;

    public AlertEngine(IEnumerable<IRule> rules)
    {
        _rules = rules.ToList();
    }

    /// <summary>
    /// Fiyat noktalarını kronolojik sırayla işler; eşleşen her kural için bir uyarı döner.
    /// </summary>
    public IEnumerable<Alert> Run(IEnumerable<PricePoint> prices)
    {
        PricePoint? previous = null;

        foreach (var current in prices)
        {
            foreach (var rule in _rules)
            {
                var alert = rule.Evaluate(current, previous);
                if (alert is not null)
                    yield return alert;
            }

            previous = current;
        }
    }
}
