using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGInsurance.Application.DTOs;
using SGInsurance.Application.Interfaces;
using SGInsurance.Application.Rating;
using SGInsurance.Domain.Entities;

namespace SGInsurance.Application.Services;

public interface IQuoteService
{
    Task<QuoteResponse> CreateAsync(CreateQuoteRequest request);
    Task<List<QuoteResponse>> GetAllAsync();
    Task<QuoteResponse?> GetByIdAsync(Guid quoteId);
    Task<List<QuoteResponse>> GetByCustomerAsync(Guid customerId);
    Task<QuoteResponse> RecalculateAsync(Guid quoteId);
    Task DeleteAsync(Guid quoteId);
}

public class QuoteService : IQuoteService
{
    private readonly IQuoteRepository _quotes;
    private readonly IProductRepository _products;
    private readonly IProductAddonRepository _addons;
    private readonly IPremiumCalculationService _premium;
    private readonly INotificationService _notifications;
    private readonly ICustomerRepository _customers;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(
        IQuoteRepository quotes,
        IProductRepository products,
        IProductAddonRepository addons,
        IPremiumCalculationService premium,
        INotificationService notifications,
        ICustomerRepository customers,
        ILogger<QuoteService> logger)
    {
        _quotes = quotes;
        _products = products;
        _addons = addons;
        _premium = premium;
        _notifications = notifications;
        _customers = customers;
        _logger = logger;
    }

    public async Task<QuoteResponse> CreateAsync(CreateQuoteRequest request)
    {
        var product = await _products.GetByIdAsync(request.ProductCode)
            ?? throw new InvalidOperationException($"Product '{request.ProductCode}' not found.");

        var sumInsured = ResolveSumInsured(request.LobCode, request.ProductData);
        var selectedAddons = await ResolveAddonsAsync(request.ProductCode, request.AddOns);

        var result = await _premium.CalculateAsync(request.ProductCode, request.LobCode, sumInsured, request.ProductData, selectedAddons);

        var quote = new Quote
        {
            QuoteId = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            ProductCode = request.ProductCode,
            LobCode = request.LobCode,
            ProductData = JsonSerializer.Serialize(request.ProductData),
            SumInsured = result.SumInsured,
            BasePremium = result.BasePremium,
            AddonPremium = result.AddonPremium,
            NcbDiscount = result.Discount,
            GstAmount = result.GstAmount,
            TotalPremium = result.TotalPremium,
            Status = QuoteStatus.Created,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        foreach (var a in selectedAddons)
            quote.QuoteAddons.Add(new QuoteAddon { QuoteId = quote.QuoteId, AddonCode = a.AddonCode, ProductCode = request.ProductCode, Price = a.Price });

        await _quotes.AddAsync(quote);
        await _quotes.SaveChangesAsync();

        _logger.LogInformation("Quote {QuoteId} created for product {ProductCode} totalling {Total}", quote.QuoteId, request.ProductCode, quote.TotalPremium);

        var customer = await _customers.GetByIdAsync(request.CustomerId);
        if (customer != null)
        {
            await _notifications.SendAsync("QUOTE_CREATED", customer.CustomerId, customer.Email, new Dictionary<string, string?>
            {
                ["firstName"] = customer.FirstName,
                ["quoteId"] = quote.QuoteId.ToString(),
                ["productName"] = product.ProductName,
                ["totalPremium"] = quote.TotalPremium.ToString("0.00")
            });
        }

        return await MapToResponseAsync(quote);
    }

    public async Task<List<QuoteResponse>> GetAllAsync()
    {
        var quotes = await _quotes.Query().OrderByDescending(q => q.CreatedAt).ToListAsync();
        var result = new List<QuoteResponse>();
        foreach (var q in quotes) result.Add(await MapToResponseAsync(q));
        return result;
    }

    public async Task<QuoteResponse?> GetByIdAsync(Guid quoteId)
    {
        var quote = await _quotes.GetByIdAsync(quoteId);
        return quote == null ? null : await MapToResponseAsync(quote);
    }

    public async Task<List<QuoteResponse>> GetByCustomerAsync(Guid customerId)
    {
        var quotes = await _quotes.Query().Where(q => q.CustomerId == customerId).OrderByDescending(q => q.CreatedAt).ToListAsync();
        var result = new List<QuoteResponse>();
        foreach (var q in quotes) result.Add(await MapToResponseAsync(q));
        return result;
    }

    public async Task<QuoteResponse> RecalculateAsync(Guid quoteId)
    {
        var quote = await _quotes.GetByIdAsync(quoteId)
            ?? throw new InvalidOperationException("Quote not found.");

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(quote.ProductData) ? "{}" : quote.ProductData);
        var productData = doc.RootElement.Clone();
        var sumInsured = ResolveSumInsured(quote.LobCode, productData);

        var addonCodes = await _quotes.Query()
            .Where(q => q.QuoteId == quoteId)
            .SelectMany(q => q.QuoteAddons)
            .Select(a => a.AddonCode)
            .ToListAsync();
        var selectedAddons = await ResolveAddonsAsync(quote.ProductCode, addonCodes);

        var result = await _premium.CalculateAsync(quote.ProductCode, quote.LobCode, sumInsured, productData, selectedAddons);

        quote.SumInsured = result.SumInsured;
        quote.BasePremium = result.BasePremium;
        quote.AddonPremium = result.AddonPremium;
        quote.NcbDiscount = result.Discount;
        quote.GstAmount = result.GstAmount;
        quote.TotalPremium = result.TotalPremium;
        quote.Status = QuoteStatus.Recalculated;
        quote.UpdatedAt = DateTimeOffset.UtcNow;

        await _quotes.SaveChangesAsync();
        _logger.LogInformation("Quote {QuoteId} recalculated, new total {Total}", quoteId, quote.TotalPremium);

        return await MapToResponseAsync(quote);
    }

    public async Task DeleteAsync(Guid quoteId)
    {
        var quote = await _quotes.GetByIdAsync(quoteId);
        if (quote == null) return;
        quote.Status = QuoteStatus.Cancelled;
        await _quotes.SaveChangesAsync();
    }

    private static decimal ResolveSumInsured(string lobCode, JsonElement productData) => lobCode switch
    {
        "HOME" => productData.GetDecimalOrDefault("sumInsuredStructure") + productData.GetDecimalOrDefault("sumInsuredContents"),
        "COMMERCIAL" => productData.GetDecimalOrDefault("propertyValue", productData.GetDecimalOrDefault("sumInsured")),
        "AGRICULTURE" => productData.GetDecimalOrDefault("sumInsured", productData.GetDecimalOrDefault("areaAcresOrHeadCount") * 10000m),
        "TRAVEL" => productData.GetDecimalOrDefault("sumInsured", 500000m),
        _ => productData.GetDecimalOrDefault("sumInsured", 500000m)
    };

    private async Task<List<SelectedAddon>> ResolveAddonsAsync(string productCode, List<string> addonCodes)
    {
        if (addonCodes.Count == 0) return new List<SelectedAddon>();
        var addons = await _addons.Query()
            .Where(a => a.ProductCode == productCode && addonCodes.Contains(a.AddonCode) && a.IsActive)
            .ToListAsync();
        return addons.Select(a => new SelectedAddon { AddonCode = a.AddonCode, Price = a.BasePrice }).ToList();
    }

    private async Task<QuoteResponse> MapToResponseAsync(Quote quote)
    {
        var product = await _products.GetByIdAsync(quote.ProductCode);
        return new QuoteResponse
        {
            QuoteId = quote.QuoteId,
            ProductCode = quote.ProductCode,
            ProductName = product?.ProductName ?? quote.ProductCode,
            LobCode = quote.LobCode,
            SumInsured = quote.SumInsured,
            BasePremium = quote.BasePremium,
            AddonPremium = quote.AddonPremium,
            Discount = quote.NcbDiscount,
            GstAmount = quote.GstAmount,
            TotalPremium = quote.TotalPremium,
            Status = quote.Status,
            ExpiresAt = quote.ExpiresAt,
            CreatedAt = quote.CreatedAt
        };
    }
}
