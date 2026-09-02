using MiniAlertEngine.Models;

namespace MiniAlertEngine.Rules;

/// <summary>
/// Fiyat üst üste 'hours' kadar geçiş boyunca aynı yönde ("up" veya "down")
/// hareket ederse eşleşir. Örn. hours=5 için 6 gözlem (5 ardışık hareket) gerekir.
/// Sabit kalan bir saat seriyi kırar (sıfır hareket hiçbir yöne sayılmaz).
/// Durum, saatler arası <see cref="EvaluationContext"/> içinde tutulur.
/// </summary>
public class StreakRule : IRule
{
    public string Id { get; }
    public int Hours { get; }
    public string Direction { get; }

    public StreakRule(string id, int hours, string direction)
    {
        if (hours < 1)
            throw new ArgumentException("'hours' en az 1 olmalı.", nameof(hours));
        if (direction is not ("up" or "down"))
            throw new ArgumentException($"Yön 'up' veya 'down' olmalı, gelen: '{direction}'.", nameof(direction));

        Id = id;
        Hours = hours;
        Direction = direction;
    }

    public Alert? Evaluate(PricePoint current, PricePoint? previous) =>
        throw new InvalidOperationException(
            "StreakRule durum bilgisi gerektirir; context'li Evaluate üzerinden çağrılmalıdır.");

    public Alert? Evaluate(PricePoint current, PricePoint? previous, EvaluationContext context)
    {
        var state = context.GetState(Id, () => new StreakState());

        if (previous is not null)
        {
            var movement = Math.Sign(current.Price - previous.Price);
            var movementDirection = movement > 0 ? "up" : movement < 0 ? "down" : null;

            if (movementDirection is not null && movementDirection == state.Direction)
            {
                state.ConsecutiveCount++;
            }
            else
            {
                state.Direction = movementDirection;
                state.ConsecutiveCount = movementDirection is null ? 0 : 1;
            }
        }

        if (state.Direction != Direction || state.ConsecutiveCount < Hours)
            return null;

        var directionText = Direction == "up" ? "yükseliş" : "düşüş";
        var message = $"Fiyat üst üste {Hours} saattir {directionText} trendinde";
        return new Alert(current.Time, Id, message, current.Price);
    }

    private class StreakState
    {
        public string? Direction { get; set; }
        public int ConsecutiveCount { get; set; }
    }
}
