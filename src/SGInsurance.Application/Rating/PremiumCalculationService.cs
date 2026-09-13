using System.Text.Json;
using SGInsurance.Application.Interfaces;

namespace SGInsurance.Application.Rating;

/// <summary>
/// Single entry point other services/controllers call to price a quote.
/// Loads the active premium_rules row for the product, resolves the matching
/// IRatingStrategy via the factory (keyed on product_master.rating_strategy_key),
/// and delegates the actual math to that strategy.
/// </summary>
public interface IPremiumCalculationService
{
    Task<PremiumResult> CalculateAsync(string productCode, string lobCode, decimal sumInsured, JsonElement productData, List<SelectedAddon> addons);
}

public class PremiumCalculationService : IPremiumCalculationService
{
    private readonly IProductRepository _products;
    private readonly IPremiumRuleRepository _rules;
    private readonly IRatingStrategyFactory _factory;

    public PremiumCalculationService(IProductRepository products, IPremiumRuleRepository rules, IRatingStrategyFactory factory)
    {
        _products = products;
        _rules = rules;
        _factory = factory;
    }

    public async Task<PremiumResult> CalculateAsync(string productCode, string lobCode, decimal sumInsured, JsonElement productData, List<SelectedAddon> addons)
    {
        var product = await _products.GetByIdAsync(productCode)
            ?? throw new InvalidOperationException($"Product '{productCode}' not found.");

        var rule = await _rules.GetActiveForProductAsync(productCode)
            ?? throw new InvalidOperationException($"No active premium rule for product '{productCode}'.");

        var strategy = _factory.Resolve(product.RatingStrategyKey);

        using var ruleDoc = JsonDocument.Parse(string.IsNullOrWhiteSpace(rule.Config) ? "{}" : rule.Config);

        var ctx = new RatingContext
        {
            ProductCode = productCode,
            LobCode = lobCode,
            ProductData = productData,
            RuleConfig = ruleDoc.RootElement.Clone(),
            SumInsured = sumInsured,
            SelectedAddons = addons
        };

        return await strategy.CalculateAsync(ctx);
    }
}
