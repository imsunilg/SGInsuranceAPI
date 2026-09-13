using System.Text.Json;

namespace SGInsurance.Application.Rating;

/// <summary>
/// Shared helpers: every strategy reads its numeric knobs from premium_rules.config
/// (ctx.RuleConfig) rather than hardcoding rates, falling back to safe defaults only
/// if a knob is missing so the engine never throws on slightly incomplete config.
/// </summary>
internal static class RatingHelpers
{
    public static decimal AddonTotal(RatingContext ctx) => ctx.SelectedAddons.Sum(a => a.Price);

    public static PremiumResult Finalize(RatingContext ctx, decimal riskPremium, decimal minPremium, decimal gstPercent, decimal discount = 0)
    {
        var addonPremium = AddonTotal(ctx);
        var floored = Math.Max(riskPremium, minPremium);
        var subtotal = floored + addonPremium - discount;
        if (subtotal < 0) subtotal = 0;
        var gst = Math.Round(subtotal * gstPercent / 100m, 2);
        var total = subtotal + gst;
        return new PremiumResult
        {
            SumInsured = ctx.SumInsured,
            BasePremium = Math.Round(floored, 2),
            AddonPremium = Math.Round(addonPremium, 2),
            Discount = Math.Round(discount, 2),
            GstAmount = gst,
            TotalPremium = Math.Round(total, 2)
        };
    }
}

public class MotorRatingStrategy : IRatingStrategy
{
    public string Key => "MOTOR";

    public Task<PremiumResult> CalculateAsync(RatingContext ctx)
    {
        var cfg = ctx.RuleConfig;
        var baseRatePercent = cfg.GetDecimalOrDefault("baseRatePercentOfSumInsured", 3.0m);
        var gstPercent = cfg.GetDecimalOrDefault("gstPercent", 18m);
        var ageLoadingPerYear = cfg.GetDecimalOrDefault("ageLoadingPercentPerYear", 1.5m);
        var minPremium = cfg.GetDecimalOrDefault("minPremium", 1500m);

        var basePremium = ctx.SumInsured * baseRatePercent / 100m;

        var year = ctx.ProductData.GetIntOrDefault("year", DateTime.UtcNow.Year);
        var vehicleAge = Math.Max(0, DateTime.UtcNow.Year - year);
        var ageLoading = basePremium * (ageLoadingPerYear * vehicleAge) / 100m;

        var ncbSlab = ctx.ProductData.GetIntOrDefault("ncbPercent", 0).ToString();
        decimal ncbDiscountPercent = 0;
        var ncbMap = cfg.GetObjectOrDefault("ncbSlabPercent");
        if (ncbMap is { } map && map.TryGetProperty(ncbSlab, out var slabVal) && slabVal.ValueKind == JsonValueKind.Number)
            ncbDiscountPercent = slabVal.GetDecimal();

        var riskPremium = basePremium + ageLoading;
        var discount = riskPremium * ncbDiscountPercent / 100m;

        return Task.FromResult(RatingHelpers.Finalize(ctx, riskPremium, minPremium, gstPercent, discount));
    }
}

public class HealthRatingStrategy : IRatingStrategy
{
    public string Key => "HEALTH";

    public Task<PremiumResult> CalculateAsync(RatingContext ctx)
    {
        var cfg = ctx.RuleConfig;
        var baseRatePercent = cfg.GetDecimalOrDefault("baseRatePercentOfSumInsured", 2.0m);
        var gstPercent = cfg.GetDecimalOrDefault("gstPercent", 18m);
        var preExistingLoadPercent = cfg.GetDecimalOrDefault("preExistingLoadPercent", 15m);
        var minPremium = cfg.GetDecimalOrDefault("minPremium", 2000m);

        var basePremium = ctx.SumInsured * baseRatePercent / 100m;

        var maxAge = 0;
        var members = ctx.ProductData.GetArrayOrDefault("members");
        if (members is { } arr)
        {
            foreach (var m in arr.EnumerateArray())
            {
                if (m.TryGetProperty("dob", out var dobEl) && dobEl.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(dobEl.GetString(), out var dob))
                {
                    var age = DateTime.UtcNow.Year - dob.Year;
                    if (age > maxAge) maxAge = age;
                }
            }
        }

        decimal ageLoadPercent = 0;
        var bands = cfg.GetArrayOrDefault("ageBands");
        if (bands is { } bandsArr)
        {
            foreach (var band in bandsArr.EnumerateArray())
            {
                var maxBandAge = band.GetIntOrDefault("maxAge", int.MaxValue);
                if (maxAge <= maxBandAge)
                {
                    ageLoadPercent = band.GetDecimalOrDefault("loadPercent", 0);
                    break;
                }
            }
        }

        var preExisting = ctx.ProductData.GetBoolOrDefault("preExistingConditions", false);
        var totalLoadPercent = ageLoadPercent + (preExisting ? preExistingLoadPercent : 0);

        var riskPremium = basePremium * (1 + totalLoadPercent / 100m);

        return Task.FromResult(RatingHelpers.Finalize(ctx, riskPremium, minPremium, gstPercent));
    }
}

public class TravelRatingStrategy : IRatingStrategy
{
    public string Key => "TRAVEL";

    public Task<PremiumResult> CalculateAsync(RatingContext ctx)
    {
        var cfg = ctx.RuleConfig;
        var baseRatePerDay = cfg.GetDecimalOrDefault("baseRatePerDay", 50m);
        var gstPercent = cfg.GetDecimalOrDefault("gstPercent", 18m);
        var internationalMultiplier = cfg.GetDecimalOrDefault("internationalMultiplier", 2.5m);
        var perTravellerFlat = cfg.GetDecimalOrDefault("perTravellerFlat", 100m);
        var minPremium = cfg.GetDecimalOrDefault("minPremium", 500m);

        var start = ctx.ProductData.GetStringOrDefault("tripStartDate");
        var end = ctx.ProductData.GetStringOrDefault("tripEndDate");
        var days = 1;
        if (DateTime.TryParse(start, out var s) && DateTime.TryParse(end, out var e) && e > s)
            days = Math.Max(1, (e - s).Days);

        var travellers = Math.Max(1, ctx.ProductData.GetIntOrDefault("numberOfTravellers", 1));
        var purpose = ctx.ProductData.GetStringOrDefault("purpose", "") ?? "";
        var isInternational = ctx.LobCode == "TRAVEL" &&
            (ctx.ProductData.GetArrayOrDefault("destinations") is { } dests &&
             dests.EnumerateArray().Any(d => !string.Equals(d.GetString(), "INDIA", StringComparison.OrdinalIgnoreCase)));

        var basePremium = baseRatePerDay * days * travellers;
        if (isInternational) basePremium *= internationalMultiplier;
        basePremium += perTravellerFlat * travellers;

        return Task.FromResult(RatingHelpers.Finalize(ctx, basePremium, minPremium, gstPercent));
    }
}

public class HomeRatingStrategy : IRatingStrategy
{
    public string Key => "HOME";

    public Task<PremiumResult> CalculateAsync(RatingContext ctx)
    {
        var cfg = ctx.RuleConfig;
        var baseRatePercent = cfg.GetDecimalOrDefault("baseRatePercentOfSumInsured", 0.15m);
        var gstPercent = cfg.GetDecimalOrDefault("gstPercent", 18m);
        var propertyAgeLoadingPerYear = cfg.GetDecimalOrDefault("propertyAgeLoadingPercentPerYear", 0.5m);
        var minPremium = cfg.GetDecimalOrDefault("minPremium", 1000m);

        var basePremium = ctx.SumInsured * baseRatePercent / 100m;
        var propertyAge = ctx.ProductData.GetIntOrDefault("propertyAge", 0);
        var ageLoading = basePremium * (propertyAgeLoadingPerYear * propertyAge) / 100m;

        var riskPremium = basePremium + ageLoading;

        return Task.FromResult(RatingHelpers.Finalize(ctx, riskPremium, minPremium, gstPercent));
    }
}

public class PersonalAccidentRatingStrategy : IRatingStrategy
{
    public string Key => "PERSONAL_ACCIDENT";

    public Task<PremiumResult> CalculateAsync(RatingContext ctx)
    {
        var cfg = ctx.RuleConfig;
        var baseRatePercent = cfg.GetDecimalOrDefault("baseRatePercentOfSumInsured", 0.5m);
        var gstPercent = cfg.GetDecimalOrDefault("gstPercent", 18m);
        var minPremium = cfg.GetDecimalOrDefault("minPremium", 500m);

        var basePremium = ctx.SumInsured * baseRatePercent / 100m;

        var occupation = ctx.ProductData.GetStringOrDefault("occupationClass", "CLERICAL") ?? "CLERICAL";
        decimal occupationLoadPercent = 0;
        var occMap = cfg.GetObjectOrDefault("occupationLoadPercent");
        if (occMap is { } map && map.TryGetProperty(occupation, out var val) && val.ValueKind == JsonValueKind.Number)
            occupationLoadPercent = val.GetDecimal();

        var familyMembers = ctx.ProductData.GetArrayOrDefault("familyMembers");
        var memberCount = familyMembers is { } fam ? fam.GetArrayLength() : 0;
        var familyMultiplier = 1 + memberCount; // owner + each family member gets full cover in this simplified model

        var riskPremium = basePremium * (1 + occupationLoadPercent / 100m) * familyMultiplier;

        return Task.FromResult(RatingHelpers.Finalize(ctx, riskPremium, minPremium, gstPercent));
    }
}

public class CommercialRatingStrategy : IRatingStrategy
{
    public string Key => "COMMERCIAL";

    public Task<PremiumResult> CalculateAsync(RatingContext ctx)
    {
        var cfg = ctx.RuleConfig;
        var baseRatePercent = cfg.GetDecimalOrDefault("baseRatePercentOfSumInsured", 0.8m);
        var gstPercent = cfg.GetDecimalOrDefault("gstPercent", 18m);
        var employeeLoadingPer100 = cfg.GetDecimalOrDefault("employeeCountLoadingPer100", 5m);
        var minPremium = cfg.GetDecimalOrDefault("minPremium", 5000m);

        var basePremium = ctx.SumInsured * baseRatePercent / 100m;
        var employees = ctx.ProductData.GetIntOrDefault("numberOfEmployees", 0);
        var loading = basePremium * (employeeLoadingPer100 * (employees / 100m)) / 100m;

        var riskPremium = basePremium + loading;

        return Task.FromResult(RatingHelpers.Finalize(ctx, riskPremium, minPremium, gstPercent));
    }
}

public class AgricultureRatingStrategy : IRatingStrategy
{
    public string Key => "AGRICULTURE";

    public Task<PremiumResult> CalculateAsync(RatingContext ctx)
    {
        var cfg = ctx.RuleConfig;
        var baseRatePercent = cfg.GetDecimalOrDefault("baseRatePercentOfSumInsured", 5.0m);
        var gstPercent = cfg.GetDecimalOrDefault("gstPercent", 18m);
        var minPremium = cfg.GetDecimalOrDefault("minPremium", 300m);

        var basePremium = ctx.SumInsured * baseRatePercent / 100m;

        var season = ctx.ProductData.GetStringOrDefault("season", "KHARIF") ?? "KHARIF";
        decimal seasonLoadPercent = 0;
        var seasonMap = cfg.GetObjectOrDefault("seasonLoadPercent");
        if (seasonMap is { } map && map.TryGetProperty(season, out var val) && val.ValueKind == JsonValueKind.Number)
            seasonLoadPercent = val.GetDecimal();

        var riskPremium = basePremium * (1 + seasonLoadPercent / 100m);

        return Task.FromResult(RatingHelpers.Finalize(ctx, riskPremium, minPremium, gstPercent));
    }
}

public class CyberRatingStrategy : IRatingStrategy
{
    public string Key => "CYBER";

    public Task<PremiumResult> CalculateAsync(RatingContext ctx)
    {
        var cfg = ctx.RuleConfig;
        var baseRatePercent = cfg.GetDecimalOrDefault("baseRatePercentOfSumInsured", 1.0m);
        var gstPercent = cfg.GetDecimalOrDefault("gstPercent", 18m);
        var volumeLoadPercent = cfg.GetDecimalOrDefault("volumeLoadPercent", 10m);
        var minPremium = cfg.GetDecimalOrDefault("minPremium", 800m);

        var basePremium = ctx.SumInsured * baseRatePercent / 100m;
        var riskPremium = basePremium * (1 + volumeLoadPercent / 100m);

        return Task.FromResult(RatingHelpers.Finalize(ctx, riskPremium, minPremium, gstPercent));
    }
}
