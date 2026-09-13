using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGInsurance.Application.DTOs;
using SGInsurance.Application.Services;

namespace SGInsurance.Api.Controllers;

[Route("api/v1")]
public class ProductsController : ApiControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("lobs")]
    public async Task<ActionResult> GetLobs() => Ok(await _productService.GetLobsAsync());

    [HttpGet("products")]
    public async Task<ActionResult> GetProducts([FromQuery] string? lobCode) => Ok(await _productService.GetProductsAsync(lobCode));

    [HttpGet("products/{productCode}")]
    public async Task<ActionResult> GetProduct(string productCode)
    {
        var product = await _productService.GetProductAsync(productCode);
        if (product == null) return NotFound(Application.Common.ApiResponse<object?>.Fail("Product not found."));
        return Ok(product);
    }

    [HttpPost("products")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> CreateProduct(UpsertProductRequest request) => Ok(await _productService.UpsertProductAsync(request), "Product created.");

    [HttpPut("products/{productCode}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> UpdateProduct(string productCode, UpsertProductRequest request)
    {
        request.ProductCode = productCode;
        return Ok(await _productService.UpsertProductAsync(request), "Product updated.");
    }

    [HttpDelete("products/{productCode}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> DeleteProduct(string productCode)
    {
        await _productService.DeleteProductAsync(productCode);
        return Ok<object?>(null, "Product deactivated.");
    }
}
