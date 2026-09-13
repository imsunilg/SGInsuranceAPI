using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGInsurance.Application.DTOs;
using SGInsurance.Application.Services;

namespace SGInsurance.Api.Controllers;

[Route("api/v1/payments")]
[Authorize]
public class PaymentsController : ApiControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IValidator<CreatePaymentRequest> _validator;

    public PaymentsController(IPaymentService paymentService, IValidator<CreatePaymentRequest> validator)
    {
        _paymentService = paymentService;
        _validator = validator;
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreatePaymentRequest request)
    {
        await ValidateAsync(_validator, request);
        return Ok(await _paymentService.CreateAsync(request), "Payment processed.");
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var payment = await _paymentService.GetByIdAsync(id);
        if (payment == null) return NotFound(Application.Common.ApiResponse<object?>.Fail("Payment not found."));
        return Ok(payment);
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<ActionResult> Retry(Guid id, RetryPaymentRequest request) => Ok(await _paymentService.RetryAsync(id, request), "Payment retried.");
}
