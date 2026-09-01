using MiniAlertEngine.Models;

namespace MiniAlertEngine.Rules;

/// <summary>
/// Fiyat belirli bir eşik değerinin üstüne çıkınca ("gt") veya altına inince ("lt") eşleşir.
/// </summary>
public class ThresholdRule : IRule
{
    public string Id { get; }
    public string Operator { get; }
    public decimal Value { get; }

    public ThresholdRule(string id, string @operator, decimal value)
    {
        if (@operator is not ("gt" or "lt"))
            throw new ArgumentException($"Threshold operatörü 'gt' veya 'lt' olmalı, gelen: '{@operator}'.", nameof(@operator));

        Id = id;
        Operator = @operator;
        Value = value;
    }

    public Alert? Evaluate(PricePoint current, PricePoint? previous)
    {
        var matched = Operator == "gt"
            ? current.Price > Value
            : current.Price < Value;

        if (!matched)
            return null;

        var message = Operator == "gt"
            ? $"Fiyat eşik değerin üstünde (>{Value})"
            : $"Fiyat eşik değerin altında (<{Value})";

        return new Alert(current.Time, Id, message, current.Price);
    }
}
