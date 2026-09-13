using System.Text.Json;

namespace SGInsurance.Application.DTOs;

public class LobDto
{
    public string LobCode { get; set; } = default!;
    public string LobName { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class ProductDto
{
    public string ProductCode { get; set; } = default!;
    public string LobCode { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public JsonElement Config { get; set; }
    public string RatingStrategyKey { get; set; } = default!;
    public List<ProductAddonDto> Addons { get; set; } = new();
}

public class ProductAddonDto
{
    public string AddonCode { get; set; } = default!;
    public string AddonName { get; set; } = default!;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
}

public class UpsertProductRequest
{
    public string ProductCode { get; set; } = default!;
    public string LobCode { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string Config { get; set; } = "{}";
    public string RatingStrategyKey { get; set; } = default!;
}
