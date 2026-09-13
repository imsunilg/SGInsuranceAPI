using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGInsurance.Application.Services;

namespace SGInsurance.Api.Controllers;

[Route("api/v1/policies")]
[Authorize]
public class PoliciesController : ApiControllerBase
{
    private readonly IPolicyService _policyService;

    public PoliciesController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> GetAll() => Ok(await _policyService.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var policy = await _policyService.GetByIdAsync(id);
        if (policy == null) return NotFound(Application.Common.ApiResponse<object?>.Fail("Policy not found."));
        return Ok(policy);
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult> GetByCustomer(Guid customerId) => Ok(await _policyService.GetByCustomerAsync(customerId));

    [HttpGet("{id:guid}/document")]
    public async Task<IActionResult> GetDocument(Guid id)
    {
        var path = await _policyService.GenerateDocumentAsync(id);
        var html = await System.IO.File.ReadAllTextAsync(path);
        return Content(html, "text/html");
    }
}
