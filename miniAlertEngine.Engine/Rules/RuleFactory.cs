namespace MiniAlertEngine.Rules;

/// <summary>
/// JSON'dan okunan ham kural tanımlarını somut <see cref="IRule"/> nesnelerine çevirir.
/// </summary>
public static class RuleFactory
{
    /// <summary>Kök seviye kural üretir; kök kuralda 'id' zorunludur.</summary>
    public static IRule Create(RuleDefinition definition) =>
        CreateCore(definition, isRoot: true);

    private static IRule CreateCore(RuleDefinition definition, bool isRoot)
    {
        if (isRoot && string.IsNullOrWhiteSpace(definition.Id))
            throw new InvalidOperationException("Kök kuralda 'id' alanı zorunludur.");

        // İç kuralların id'si yoktur; sinyal amaçlı anonim kimlik verilir.
        var id = definition.Id ?? "<anon>";

        return definition.Type switch
        {
            "threshold" => new ThresholdRule(
                id,
                definition.Operator ?? throw Missing(id, "operator"),
                definition.Value ?? throw Missing(id, "value")),

            "change" => new ChangeRule(
                id,
                definition.Percent ?? throw Missing(id, "percent")),

            "range" => new RangeRule(
                id,
                definition.Min ?? throw Missing(id, "min"),
                definition.Max ?? throw Missing(id, "max")),

            "and" => new AndRule(
                id,
                CreateChildren(definition, id)),

            "or" => new OrRule(
                id,
                CreateChildren(definition, id)),

            "not" => new NotRule(
                id,
                CreateCore(definition.Rule ?? throw Missing(id, "rule"), isRoot: false)),

            "streak" => new StreakRule(
                id,
                definition.Hours ?? throw Missing(id, "hours"),
                definition.Direction ?? throw Missing(id, "direction")),

            "cooldown" => new CooldownRule(
                id,
                definition.Hours ?? throw Missing(id, "hours"),
                CreateCore(definition.Rule ?? throw Missing(id, "rule"), isRoot: false)),

            var other => throw new InvalidOperationException(
                $"Kural '{id}': bilinmeyen tip '{other}'. Desteklenenler: threshold, change, range, and, or, not, streak, cooldown.")
        };
    }

    private static IEnumerable<IRule> CreateChildren(RuleDefinition definition, string parentId) =>
        (definition.Rules ?? throw Missing(parentId, "rules"))
            .Select(child => CreateCore(child, isRoot: false));

    private static InvalidOperationException Missing(string ruleId, string field) =>
        new($"Kural '{ruleId}': zorunlu alan '{field}' eksik.");
}
