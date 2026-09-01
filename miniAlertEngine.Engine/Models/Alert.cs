namespace MiniAlertEngine.Models;

/// <summary>
/// Bir kural eşleştiğinde üretilen uyarı.
/// </summary>
public record Alert(DateTimeOffset Time, string RuleId, string Message, decimal Price)
{
    public override string ToString() =>
        $"[{Time:yyyy-MM-dd HH:mm}] {RuleId}: {Message} (price: {Price})";
}
