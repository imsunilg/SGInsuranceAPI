namespace SGInsurance.Domain.Entities;

public class LobMaster
{
    public string LobCode { get; set; } = default!;
    public string LobName { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public ICollection<ProductMaster> Products { get; set; } = new List<ProductMaster>();
}

public class ProductMaster
{
    public string ProductCode { get; set; } = default!;
    public string LobCode { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string Config { get; set; } = "{}"; // jsonb stored as raw json string
    public string RatingStrategyKey { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public LobMaster Lob { get; set; } = default!;
    public ICollection<ProductAddon> Addons { get; set; } = new List<ProductAddon>();
    public ICollection<PremiumRule> PremiumRules { get; set; } = new List<PremiumRule>();
}

public class ProductAddon
{
    public string AddonCode { get; set; } = default!;
    public string ProductCode { get; set; } = default!;
    public string AddonName { get; set; } = default!;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;

    public ProductMaster Product { get; set; } = default!;
}

public class PremiumRule
{
    public Guid RuleId { get; set; }
    public string ProductCode { get; set; } = default!;
    public string RuleType { get; set; } = "BASELINE";
    public string Config { get; set; } = "{}";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public ProductMaster Product { get; set; } = default!;
}

public class VehicleMake
{
    public string MakeCode { get; set; } = default!;
    public string MakeName { get; set; } = default!;
}

public class VehicleModel
{
    public string ModelCode { get; set; } = default!;
    public string MakeCode { get; set; } = default!;
    public string ModelName { get; set; } = default!;
}

public class FuelType
{
    public string FuelCode { get; set; } = default!;
    public string FuelName { get; set; } = default!;
}

public class PaymentMode
{
    public string ModeCode { get; set; } = default!;
    public string ModeName { get; set; } = default!;
}

public class NotificationTemplate
{
    public string TemplateCode { get; set; } = default!;
    public string Channel { get; set; } = "EMAIL";
    public string? Subject { get; set; }
    public string BodyTemplate { get; set; } = default!;
    public string Placeholders { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
}
