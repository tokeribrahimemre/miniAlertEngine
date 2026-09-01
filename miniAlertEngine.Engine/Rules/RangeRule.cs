using MiniAlertEngine.Models;

namespace MiniAlertEngine.Rules;

/// <summary>
/// Fiyat belirtilen [min, max] bandının (sınırlar dahil) dışına çıkınca eşleşir.
/// </summary>
public class RangeRule : IRule
{
    public string Id { get; }
    public decimal Min { get; }
    public decimal Max { get; }

    public RangeRule(string id, decimal min, decimal max)
    {
        if (min > max)
            throw new ArgumentException($"'min' ({min}), 'max' ({max}) değerinden büyük olamaz.", nameof(min));

        Id = id;
        Min = min;
        Max = max;
    }

    public Alert? Evaluate(PricePoint current, PricePoint? previous)
    {
        if (current.Price >= Min && current.Price <= Max)
            return null;

        var message = $"Fiyat [{Min} - {Max}] bandının dışına çıktı";
        return new Alert(current.Time, Id, message, current.Price);
    }
}
