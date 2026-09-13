using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGInsurance.Application.DTOs;
using SGInsurance.Application.Interfaces;
using SGInsurance.Domain.Entities;

namespace SGInsurance.Application.Services;

public interface IProposalService
{
    Task<ProposalResponse> CreateAsync(CreateProposalRequest request);
    Task<ProposalResponse?> GetByIdAsync(Guid proposalId);
    Task<ProposalResponse> UpdateAsync(Guid proposalId, UpdateProposalRequest request);
    Task<ProposalResponse> SubmitAsync(Guid proposalId);
    Task<KycResultResponse> VerifyKycAsync(Guid proposalId, KycVerifyRequest request);
    Task<RiskVerificationResponse> VerifyRiskAsync(Guid proposalId, RiskVerificationRequest request);
}

/// <summary>
/// Explicit state machine for the proposal lifecycle:
/// DRAFT -> SUBMITTED -> KYC_PENDING -> VERIFICATION_PENDING -> PAYMENT_PENDING
///        -> PAYMENT_FAILED (after 3rd failed/timeout payment attempt) | COMPLETED (on payment success)
/// Any other transition is rejected with a clear error.
/// </summary>
public class ProposalService : IProposalService
{
    private static readonly Dictionary<string, string[]> AllowedTransitions = new()
    {
        [ProposalStatus.Draft] = new[] { ProposalStatus.Submitted },
        [ProposalStatus.Submitted] = new[] { ProposalStatus.KycPending },
        [ProposalStatus.KycPending] = new[] { ProposalStatus.VerificationPending, ProposalStatus.KycPending },
        [ProposalStatus.VerificationPending] = new[] { ProposalStatus.PaymentPending, ProposalStatus.VerificationPending },
        [ProposalStatus.PaymentPending] = new[] { ProposalStatus.PaymentFailed, ProposalStatus.Completed },
        [ProposalStatus.PaymentFailed] = new[] { ProposalStatus.PaymentPending },
    };

    private readonly IProposalRepository _proposals;
    private readonly IQuoteRepository _quotes;
    private readonly IKycRepository _kyc;
    private readonly IRiskVerificationRepository _risk;
    private readonly ICustomerRepository _customers;
    private readonly INotificationService _notifications;
    private readonly ILogger<ProposalService> _logger;

    public ProposalService(
        IProposalRepository proposals,
        IQuoteRepository quotes,
        IKycRepository kyc,
        IRiskVerificationRepository risk,
        ICustomerRepository customers,
        INotificationService notifications,
        ILogger<ProposalService> logger)
    {
        _proposals = proposals;
        _quotes = quotes;
        _kyc = kyc;
        _risk = risk;
        _customers = customers;
        _notifications = notifications;
        _logger = logger;
    }

    public static void EnsureTransitionAllowed(string from, string to)
    {
        if (from == to) return;
        if (!AllowedTransitions.TryGetValue(from, out var allowed) || !allowed.Contains(to))
            throw new InvalidOperationException($"Invalid proposal status transition from '{from}' to '{to}'.");
    }

    public async Task<ProposalResponse> CreateAsync(CreateProposalRequest request)
    {
        var quote = await _quotes.GetByIdAsync(request.QuoteId)
            ?? throw new InvalidOperationException("Quote not found.");

        var proposal = new Proposal
        {
            ProposalId = Guid.NewGuid(),
            QuoteId = request.QuoteId,
            CustomerId = quote.CustomerId,
            Status = ProposalStatus.Draft,
            ProposalData = JsonSerializer.Serialize(request.ProposalData),
            NomineeData = JsonSerializer.Serialize(request.NomineeData),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _proposals.AddAsync(proposal);

        quote.Status = QuoteStatus.Converted;
        await _proposals.SaveChangesAsync();

        _logger.LogInformation("Proposal {ProposalId} created from quote {QuoteId}", proposal.ProposalId, request.QuoteId);

        return MapToResponse(proposal);
    }

    public async Task<ProposalResponse?> GetByIdAsync(Guid proposalId)
    {
        var proposal = await _proposals.GetByIdAsync(proposalId);
        return proposal == null ? null : MapToResponse(proposal);
    }

    public async Task<ProposalResponse> UpdateAsync(Guid proposalId, UpdateProposalRequest request)
    {
        var proposal = await _proposals.GetByIdAsync(proposalId)
            ?? throw new InvalidOperationException("Proposal not found.");
        if (proposal.Status != ProposalStatus.Draft)
            throw new InvalidOperationException("Only DRAFT proposals can be edited.");

        proposal.ProposalData = JsonSerializer.Serialize(request.ProposalData);
        proposal.NomineeData = JsonSerializer.Serialize(request.NomineeData);
        proposal.UpdatedAt = DateTimeOffset.UtcNow;
        await _proposals.SaveChangesAsync();
        return MapToResponse(proposal);
    }

    public async Task<ProposalResponse> SubmitAsync(Guid proposalId)
    {
        var proposal = await _proposals.GetByIdAsync(proposalId)
            ?? throw new InvalidOperationException("Proposal not found.");

        EnsureTransitionAllowed(proposal.Status, ProposalStatus.Submitted);
        proposal.Status = ProposalStatus.Submitted;
        proposal.UpdatedAt = DateTimeOffset.UtcNow;

        // Immediately move into KYC_PENDING - submission implies KYC is now required.
        EnsureTransitionAllowed(proposal.Status, ProposalStatus.KycPending);
        proposal.Status = ProposalStatus.KycPending;

        await _proposals.SaveChangesAsync();
        _logger.LogInformation("Proposal {ProposalId} submitted, awaiting KYC", proposalId);

        var customer = await _customers.GetByIdAsync(proposal.CustomerId);
        if (customer != null)
        {
            await _notifications.SendAsync("PROPOSAL_SUBMITTED", customer.CustomerId, customer.Email, new Dictionary<string, string?>
            {
                ["firstName"] = customer.FirstName,
                ["proposalId"] = proposal.ProposalId.ToString()
            });
        }

        return MapToResponse(proposal);
    }

    public async Task<KycResultResponse> VerifyKycAsync(Guid proposalId, KycVerifyRequest request)
    {
        var proposal = await _proposals.GetByIdAsync(proposalId)
            ?? throw new InvalidOperationException("Proposal not found.");
        if (proposal.Status != ProposalStatus.KycPending)
            throw new InvalidOperationException($"Proposal must be in KYC_PENDING status (currently '{proposal.Status}').");

        // Deterministic dummy PAN check: valid format => VERIFIED, else FAILED.
        var panValid = System.Text.RegularExpressions.Regex.IsMatch(request.Pan, "^[A-Z]{5}[0-9]{4}[A-Z]{1}$");
        var result = panValid ? "VERIFIED" : "FAILED";

        var kyc = new KycVerification
        {
            KycId = Guid.NewGuid(),
            ProposalId = proposalId,
            Pan = request.Pan,
            Name = request.Name,
            Dob = request.Dob,
            Address = request.Address,
            Result = result,
            VerifiedAt = DateTimeOffset.UtcNow
        };
        await _kyc.AddAsync(kyc);

        if (result == "VERIFIED")
        {
            EnsureTransitionAllowed(proposal.Status, ProposalStatus.VerificationPending);
            proposal.Status = ProposalStatus.VerificationPending;
            proposal.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _kyc.SaveChangesAsync();
        _logger.LogInformation("KYC {Result} for proposal {ProposalId}", result, proposalId);

        if (result == "VERIFIED")
        {
            var customer = await _customers.GetByIdAsync(proposal.CustomerId);
            if (customer != null)
            {
                await _notifications.SendAsync("KYC_VERIFIED", customer.CustomerId, customer.Email, new Dictionary<string, string?>
                {
                    ["firstName"] = customer.FirstName,
                    ["proposalId"] = proposal.ProposalId.ToString()
                });
            }
        }

        return new KycResultResponse { KycId = kyc.KycId, Result = result, ProposalStatus = proposal.Status };
    }

    private static readonly HashSet<string> AllowedVerificationTypes = new()
    {
        "VEHICLE", "PROPERTY", "HEALTH_DECLARATION", "BUSINESS", "CROP", "LIVESTOCK", "TRIP"
    };

    public async Task<RiskVerificationResponse> VerifyRiskAsync(Guid proposalId, RiskVerificationRequest request)
    {
        var proposal = await _proposals.GetByIdAsync(proposalId)
            ?? throw new InvalidOperationException("Proposal not found.");
        if (proposal.Status != ProposalStatus.VerificationPending)
            throw new InvalidOperationException($"Proposal must be in VERIFICATION_PENDING status (currently '{proposal.Status}').");

        if (!AllowedVerificationTypes.Contains(request.VerificationType))
            throw new InvalidOperationException($"Unknown verification type '{request.VerificationType}'.");

        // Deterministic dummy rule: PASSED unless the client explicitly sets forceFail=true.
        var forceFail = request.VerificationData.ValueKind == JsonValueKind.Object &&
            request.VerificationData.TryGetProperty("forceFail", out var ff) && ff.ValueKind == JsonValueKind.True;
        var status = forceFail ? "FAILED" : "PASSED";

        var verification = new RiskVerification
        {
            VerificationId = Guid.NewGuid(),
            ProposalId = proposalId,
            VerificationType = request.VerificationType,
            VerificationData = JsonSerializer.Serialize(request.VerificationData),
            Status = status,
            VerifiedAt = DateTimeOffset.UtcNow
        };
        await _risk.AddAsync(verification);

        if (status == "PASSED")
        {
            EnsureTransitionAllowed(proposal.Status, ProposalStatus.PaymentPending);
            proposal.Status = ProposalStatus.PaymentPending;
            proposal.PaymentDueAt = DateTimeOffset.UtcNow.AddDays(7);
            proposal.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _risk.SaveChangesAsync();
        _logger.LogInformation("Risk verification {Status} for proposal {ProposalId} ({Type})", status, proposalId, request.VerificationType);

        return new RiskVerificationResponse { VerificationId = verification.VerificationId, Status = status, ProposalStatus = proposal.Status };
    }

    private static ProposalResponse MapToResponse(Proposal p) => new()
    {
        ProposalId = p.ProposalId,
        QuoteId = p.QuoteId,
        CustomerId = p.CustomerId,
        Status = p.Status,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
