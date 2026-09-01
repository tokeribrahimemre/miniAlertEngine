namespace MiniAlertEngine.Rules;

/// <summary>
/// JSON'dan okunan ham kural tanımlarını somut <see cref="IRule"/> nesnelerine çevirir.
/// </summary>
public static class RuleFactory
{
    public static IRule Create(RuleDefinition definition) => definition.Type switch
    {
        "threshold" => new ThresholdRule(
            definition.Id,
            definition.Operator ?? throw Missing(definition.Id, "operator"),
            definition.Value ?? throw Missing(definition.Id, "value")),

        "change" => new ChangeRule(
            definition.Id,
            definition.Percent ?? throw Missing(definition.Id, "percent")),

        "range" => new RangeRule(
            definition.Id,
            definition.Min ?? throw Missing(definition.Id, "min"),
            definition.Max ?? throw Missing(definition.Id, "max")),

        var other => throw new InvalidOperationException(
            $"Kural '{definition.Id}': bilinmeyen tip '{other}'. Desteklenenler: threshold, change, range.")
    };

    private static InvalidOperationException Missing(string ruleId, string field) =>
        new($"Kural '{ruleId}': zorunlu alan '{field}' eksik.");
}
