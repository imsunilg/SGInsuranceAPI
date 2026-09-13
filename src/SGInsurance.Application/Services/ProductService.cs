using System.Text.Json;
using SGInsurance.Application.DTOs;
using SGInsurance.Application.Interfaces;
using SGInsurance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace SGInsurance.Application.Services;

public interface IProductService
{
    Task<List<LobDto>> GetLobsAsync();
    Task<List<ProductDto>> GetProductsAsync(string? lobCode);
    Task<ProductDto?> GetProductAsync(string productCode);
    Task<ProductDto> UpsertProductAsync(UpsertProductRequest request);
    Task DeleteProductAsync(string productCode);
}

public class ProductService : IProductService
{
    private readonly ILobRepository _lobs;
    private readonly IProductRepository _products;
    private readonly IProductAddonRepository _addons;

    public ProductService(ILobRepository lobs, IProductRepository products, IProductAddonRepository addons)
    {
        _lobs = lobs;
        _products = products;
        _addons = addons;
    }

    public async Task<List<LobDto>> GetLobsAsync() =>
        await _lobs.Query().OrderBy(l => l.LobCode)
            .Select(l => new LobDto { LobCode = l.LobCode, LobName = l.LobName, IsActive = l.IsActive })
            .ToListAsync();

    public async Task<List<ProductDto>> GetProductsAsync(string? lobCode)
    {
        var query = _products.Query().Include(p => p.Addons).AsQueryable();
        if (!string.IsNullOrWhiteSpace(lobCode))
            query = query.Where(p => p.LobCode == lobCode);

        var products = await query.OrderBy(p => p.ProductCode).ToListAsync();
        return products.Select(MapToDto).ToList();
    }

    public async Task<ProductDto?> GetProductAsync(string productCode)
    {
        var product = await _products.Query().Include(p => p.Addons)
            .FirstOrDefaultAsync(p => p.ProductCode == productCode);
        return product == null ? null : MapToDto(product);
    }

    public async Task<ProductDto> UpsertProductAsync(UpsertProductRequest request)
    {
        var existing = await _products.GetByIdAsync(request.ProductCode);
        if (existing == null)
        {
            existing = new ProductMaster
            {
                ProductCode = request.ProductCode,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _products.AddAsync(existing);
        }

        existing.LobCode = request.LobCode;
        existing.ProductName = request.ProductName;
        existing.Description = request.Description;
        existing.IsActive = request.IsActive;
        existing.Config = request.Config;
        existing.RatingStrategyKey = request.RatingStrategyKey;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _products.SaveChangesAsync();
        return MapToDto(existing);
    }

    public async Task DeleteProductAsync(string productCode)
    {
        var existing = await _products.GetByIdAsync(productCode);
        if (existing == null) return;
        existing.IsActive = false; // soft delete - keeps historical quotes/policies intact
        await _products.SaveChangesAsync();
    }

    private static ProductDto MapToDto(ProductMaster p) => new()
    {
        ProductCode = p.ProductCode,
        LobCode = p.LobCode,
        ProductName = p.ProductName,
        Description = p.Description,
        IsActive = p.IsActive,
        Config = JsonDocument.Parse(string.IsNullOrWhiteSpace(p.Config) ? "{}" : p.Config).RootElement.Clone(),
        RatingStrategyKey = p.RatingStrategyKey,
        Addons = p.Addons.Where(a => a.IsActive).Select(a => new ProductAddonDto
        {
            AddonCode = a.AddonCode,
            AddonName = a.AddonName,
            Description = a.Description,
            BasePrice = a.BasePrice
        }).ToList()
    };
}
