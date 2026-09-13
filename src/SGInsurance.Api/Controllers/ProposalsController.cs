using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGInsurance.Application.DTOs;
using SGInsurance.Application.Services;

namespace SGInsurance.Api.Controllers;

[Route("api/v1/proposals")]
[Authorize]
public class ProposalsController : ApiControllerBase
{
    private readonly IProposalService _proposalService;
    private readonly IValidator<KycVerifyRequest> _kycValidator;

    public ProposalsController(IProposalService proposalService, IValidator<KycVerifyRequest> kycValidator)
    {
        _proposalService = proposalService;
        _kycValidator = kycValidator;
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateProposalRequest request) => Ok(await _proposalService.CreateAsync(request), "Proposal created.");

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var proposal = await _proposalService.GetByIdAsync(id);
        if (proposal == null) return NotFound(Application.Common.ApiResponse<object?>.Fail("Proposal not found."));
        return Ok(proposal);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateProposalRequest request) => Ok(await _proposalService.UpdateAsync(id, request), "Proposal updated.");

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult> Submit(Guid id) => Ok(await _proposalService.SubmitAsync(id), "Proposal submitted.");

    [HttpPost("{id:guid}/kyc/verify")]
    public async Task<ActionResult> VerifyKyc(Guid id, KycVerifyRequest request)
    {
        await ValidateAsync(_kycValidator, request);
        return Ok(await _proposalService.VerifyKycAsync(id, request), "KYC processed.");
    }

    [HttpPost("{id:guid}/verification/verify")]
    public async Task<ActionResult> VerifyRisk(Guid id, RiskVerificationRequest request) =>
        Ok(await _proposalService.VerifyRiskAsync(id, request), "Risk verification processed.");
}
