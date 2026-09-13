using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGInsurance.Application.DTOs;
using SGInsurance.Application.Services;

namespace SGInsurance.Api.Controllers;

[Route("api/v1/quotes")]
[Authorize]
public class QuotesController : ApiControllerBase
{
    private readonly IQuoteService _quoteService;
    private readonly IValidator<CreateQuoteRequest> _validator;

    public QuotesController(IQuoteService quoteService, IValidator<CreateQuoteRequest> validator)
    {
        _quoteService = quoteService;
        _validator = validator;
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateQuoteRequest request)
    {
        await ValidateAsync(_validator, request);
        return Ok(await _quoteService.CreateAsync(request), "Quote created.");
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> GetAll() => Ok(await _quoteService.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var quote = await _quoteService.GetByIdAsync(id);
        if (quote == null) return NotFound(Application.Common.ApiResponse<object?>.Fail("Quote not found."));
        return Ok(quote);
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult> GetByCustomer(Guid customerId) => Ok(await _quoteService.GetByCustomerAsync(customerId));

    [HttpPost("{id:guid}/recalculate")]
    public async Task<ActionResult> Recalculate(Guid id) => Ok(await _quoteService.RecalculateAsync(id), "Quote recalculated.");

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _quoteService.DeleteAsync(id);
        return Ok<object?>(null, "Quote cancelled.");
    }
}
