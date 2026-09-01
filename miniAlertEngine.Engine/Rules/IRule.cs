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
    /// Verilen fiyat noktasını değerlendirir.
    /// </summary>
    /// <param name="current">İncelenen saatlik fiyat.</param>
    /// <param name="previous">Bir önceki saatin fiyatı; ilk saat için <c>null</c>.</param>
    /// <returns>Eşleşme varsa <see cref="Alert"/>, yoksa <c>null</c>.</returns>
    Alert? Evaluate(PricePoint current, PricePoint? previous);
}
