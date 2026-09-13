using System.Text.Json;

namespace SGInsurance.Application.DTOs;

public class CreateQuoteRequest
{
    public Guid CustomerId { get; set; }
    public string ProductCode { get; set; } = default!;
    public string LobCode { get; set; } = default!;
    public JsonElement ProductData { get; set; }
    public List<string> AddOns { get; set; } = new();
}

public class QuoteResponse
{
    public Guid QuoteId { get; set; }
    public string ProductCode { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public string LobCode { get; set; } = default!;
    public decimal SumInsured { get; set; }
    public decimal BasePremium { get; set; }
    public decimal AddonPremium { get; set; }
    public decimal Discount { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalPremium { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
