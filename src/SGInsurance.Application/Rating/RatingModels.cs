using System.Text.Json;

namespace SGInsurance.Application.Rating;

/// <summary>
/// Input context handed to an IRatingStrategy. productData/ruleConfig are raw JSON
/// (as stored in postgres jsonb columns) parsed lazily via JsonDocument for flexibility -
/// each LOB strategy knows which keys to look for in its own productData shape.
/// </summary>
public class RatingContext
{
    public string ProductCode { get; set; } = default!;
    public string LobCode { get; set; } = default!;
    public JsonElement ProductData { get; set; }
    public JsonElement RuleConfig { get; set; }
    public decimal SumInsured { get; set; }
    public List<SelectedAddon> SelectedAddons { get; set; } = new();
}

public class SelectedAddon
{
    public string AddonCode { get; set; } = default!;
    public decimal Price { get; set; }
}

public class PremiumResult
{
    public decimal SumInsured { get; set; }
    public decimal BasePremium { get; set; }
    public decimal AddonPremium { get; set; }
    public decimal Discount { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalPremium { get; set; }
}

public static class JsonElementExtensions
{
    public static decimal GetDecimalOrDefault(this JsonElement el, string prop, decimal fallback = 0)
    {
        if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var v))
        {
            if (v.ValueKind == JsonValueKind.Number) return v.GetDecimal();
            if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(), out var d)) return d;
        }
        return fallback;
    }

    public static int GetIntOrDefault(this JsonElement el, string prop, int fallback = 0)
    {
        if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var v))
        {
            if (v.ValueKind == JsonValueKind.Number) return v.GetInt32();
            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var i)) return i;
        }
        return fallback;
    }

    public static string? GetStringOrDefault(this JsonElement el, string prop, string? fallback = null)
    {
        if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String)
            return v.GetString();
        return fallback;
    }

    public static bool GetBoolOrDefault(this JsonElement el, string prop, bool fallback = false)
    {
        if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var v))
        {
            if (v.ValueKind == JsonValueKind.True) return true;
            if (v.ValueKind == JsonValueKind.False) return false;
        }
        return fallback;
    }

    public static JsonElement? GetObjectOrDefault(this JsonElement el, string prop)
    {
        if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Object)
            return v;
        return null;
    }

    public static JsonElement? GetArrayOrDefault(this JsonElement el, string prop)
    {
        if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Array)
            return v;
        return null;
    }
}
