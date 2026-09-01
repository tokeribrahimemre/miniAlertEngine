namespace MiniAlertEngine.Models;

/// <summary>
/// Tek bir saatlik fiyat gözlemi.
/// </summary>
public record PricePoint(DateTimeOffset Time, decimal Price);
