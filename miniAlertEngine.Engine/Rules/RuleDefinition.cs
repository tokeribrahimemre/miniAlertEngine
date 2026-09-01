using System.Text.Json.Serialization;

namespace MiniAlertEngine.Rules;

/// <summary>
/// rules.json içindeki tek bir kural tanımının ham hali.
/// "type" alanına göre ilgili kural sınıfına dönüştürülür.
/// </summary>
public class RuleDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>threshold: "gt" veya "lt".</summary>
    [JsonPropertyName("operator")]
    public string? Operator { get; init; }

    /// <summary>threshold: eşik değer.</summary>
    [JsonPropertyName("value")]
    public decimal? Value { get; init; }

    /// <summary>change: yüzde eşiği.</summary>
    [JsonPropertyName("percent")]
    public decimal? Percent { get; init; }

    /// <summary>range: bandın alt sınırı.</summary>
    [JsonPropertyName("min")]
    public decimal? Min { get; init; }

    /// <summary>range: bandın üst sınırı.</summary>
    [JsonPropertyName("max")]
    public decimal? Max { get; init; }
}
