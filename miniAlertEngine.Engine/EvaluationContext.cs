namespace MiniAlertEngine;

/// <summary>
/// Saatler arası durum (state) taşıyan kurallar için değerlendirme bağlamı.
/// Motor her çalıştırmada yeni bir bağlam oluşturur; state kural kimliği
/// başına izole tutulur, böylece kurallar birbirinin durumunu kirletmez.
/// </summary>
public class EvaluationContext
{
    private readonly Dictionary<string, object> _states = new();

    /// <summary>
    /// Verilen kural kimliğine ait state'i döner; yoksa <paramref name="factory"/>
    /// ile oluşturup kaydeder.
    /// </summary>
    public T GetState<T>(string ruleId, Func<T> factory) where T : class
    {
        if (_states.TryGetValue(ruleId, out var existing))
            return (T)existing;

        var created = factory();
        _states[ruleId] = created;
        return created;
    }
}
