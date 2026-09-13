namespace SGInsurance.Application.Rating;

/// <summary>
/// One implementation per LOB. Resolved via IRatingStrategyFactory keyed on
/// product_master.rating_strategy_key (MOTOR/HEALTH/TRAVEL/HOME/PERSONAL_ACCIDENT/
/// COMMERCIAL/AGRICULTURE/CYBER).
/// </summary>
public interface IRatingStrategy
{
    string Key { get; }
    Task<PremiumResult> CalculateAsync(RatingContext ctx);
}

public interface IRatingStrategyFactory
{
    IRatingStrategy Resolve(string ratingStrategyKey);
}

public class RatingStrategyFactory : IRatingStrategyFactory
{
    private readonly Dictionary<string, IRatingStrategy> _strategies;

    public RatingStrategyFactory(IEnumerable<IRatingStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.Key, s => s, StringComparer.OrdinalIgnoreCase);
    }

    public IRatingStrategy Resolve(string ratingStrategyKey)
    {
        if (_strategies.TryGetValue(ratingStrategyKey, out var strategy))
            return strategy;
        throw new InvalidOperationException($"No rating strategy registered for key '{ratingStrategyKey}'.");
    }
}
