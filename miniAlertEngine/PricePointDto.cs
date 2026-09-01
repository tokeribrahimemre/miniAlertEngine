using System.Text.Json.Serialization;
using MiniAlertEngine.Models;

namespace MiniAlertEngine.Cli;

/// <summary>
/// prices.json içindeki tek bir kaydın ham hali.
/// </summary>
public class PricePointDto
{
    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; init; }

    [JsonPropertyName("price")]
    public decimal Price { get; init; }

    public PricePoint ToModel() => new(Time, Price);
}
