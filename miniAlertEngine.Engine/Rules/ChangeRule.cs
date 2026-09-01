using MiniAlertEngine.Models;

namespace MiniAlertEngine.Rules;

/// <summary>
/// Fiyat bir önceki saate göre yüzde olarak belirli bir miktarda (veya daha fazla)
/// değiştiğinde eşleşir. Yön önemli değildir; mutlak değişim kullanılır.
/// İlk saat için önceki fiyat bulunmadığından kural eşleşmez (sessiz başlangıç).
/// </summary>
public class ChangeRule : IRule
{
    public string Id { get; }
    public decimal Percent { get; }

    public ChangeRule(string id, decimal percent)
    {
        if (percent < 0)
            throw new ArgumentException("Yüzde değeri negatif olamaz.", nameof(percent));

        Id = id;
        Percent = percent;
    }

    public Alert? Evaluate(PricePoint current, PricePoint? previous)
    {
        // İlk saat: karşılaştırma yapılacak önceki fiyat yok, eşleşme üretme.
        if (previous is null || previous.Price == 0)
            return null;

        var changePercent = (current.Price - previous.Price) / previous.Price * 100m;

        if (Math.Abs(changePercent) < Percent)
            return null;

        var direction = changePercent >= 0 ? "artış" : "düşüş";
        var message = $"Fiyat bir saatte %{Math.Round(Math.Abs(changePercent), 2)} {direction} gösterdi (eşik: %{Percent})";

        return new Alert(current.Time, Id, message, current.Price);
    }
}
