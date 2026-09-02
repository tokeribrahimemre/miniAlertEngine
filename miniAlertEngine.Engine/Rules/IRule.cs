using MiniAlertEngine.Models;

namespace MiniAlertEngine.Rules;

/// <summary>
/// Fiyat akışına uygulanabilen bir alarm kuralı.
/// </summary>
public interface IRule
{
    /// <summary>Kuralın benzersiz kimliği (JSON'daki "id").</summary>
    string Id { get; }

    /// <summary>
    /// Verilen fiyat noktasını değerlendirir (durumsuz kurallar için).
    /// </summary>
    /// <param name="current">İncelenen saatlik fiyat.</param>
    /// <param name="previous">Bir önceki saatin fiyatı; ilk saat için <c>null</c>.</param>
    /// <returns>Eşleşme varsa <see cref="Alert"/>, yoksa <c>null</c>.</returns>
    Alert? Evaluate(PricePoint current, PricePoint? previous);

    /// <summary>
    /// Verilen fiyat noktasını, saatler arası durum taşıyan bağlam ile değerlendirir.
    /// Varsayılan implementasyon bağlamı yok sayar; state tutan kurallar
    /// (ör. streak, cooldown) bu metodu geçersiz kılar.
    /// </summary>
    Alert? Evaluate(PricePoint current, PricePoint? previous, EvaluationContext context)
        => Evaluate(current, previous);
}
