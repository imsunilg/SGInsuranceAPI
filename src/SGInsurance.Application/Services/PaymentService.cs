using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGInsurance.Application.DTOs;
using SGInsurance.Application.Interfaces;
using SGInsurance.Domain.Entities;

namespace SGInsurance.Application.Services;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(CreatePaymentRequest request);
    Task<PaymentResponse?> GetByIdAsync(Guid paymentId);
    Task<PaymentResponse> RetryAsync(Guid paymentId, RetryPaymentRequest request);
}

/// <summary>
/// Dummy payment gateway. Accepts an explicit simulateResult from the client
/// (matching the frontend's Simulate Success/Failure/Timeout buttons); falls back
/// to SUCCESS if none given. Max 3 attempts tracked in payment_attempts - on the
/// 3rd failed/timeout attempt the proposal moves to PAYMENT_FAILED; on SUCCESS at
/// any attempt the policy is issued and the proposal moves to COMPLETED.
/// </summary>
public class PaymentService : IPaymentService
{
    public const int MaxAttempts = 3;

    private readonly IPaymentRepository _payments;
    private readonly IPaymentAttemptRepository _attempts;
    private readonly IProposalRepository _proposals;
    private readonly ICustomerRepository _customers;
    private readonly IPolicyService _policyService;
    private readonly INotificationService _notifications;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository payments,
        IPaymentAttemptRepository attempts,
        IProposalRepository proposals,
        ICustomerRepository customers,
        IPolicyService policyService,
        INotificationService notifications,
        ILogger<PaymentService> logger)
    {
        _payments = payments;
        _attempts = attempts;
        _proposals = proposals;
        _customers = customers;
        _policyService = policyService;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<PaymentResponse> CreateAsync(CreatePaymentRequest request)
    {
        var proposal = await _proposals.GetByIdAsync(request.ProposalId)
            ?? throw new InvalidOperationException("Proposal not found.");
        if (proposal.Status != ProposalStatus.PaymentPending)
            throw new InvalidOperationException($"Proposal must be in PAYMENT_PENDING status (currently '{proposal.Status}').");

        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            ProposalId = request.ProposalId,
            Amount = request.Amount,
            Mode = request.Mode,
            Status = "INITIATED",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _payments.AddAsync(payment);
        await _payments.SaveChangesAsync();

        return await ProcessAttemptAsync(payment, proposal, request.SimulateResult);
    }

    public async Task<PaymentResponse?> GetByIdAsync(Guid paymentId)
    {
        var payment = await _payments.Query().Include(p => p.Attempts).FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        if (payment == null) return null;
        return await MapToResponseAsync(payment);
    }

    public async Task<PaymentResponse> RetryAsync(Guid paymentId, RetryPaymentRequest request)
    {
        var payment = await _payments.Query().Include(p => p.Attempts).FirstOrDefaultAsync(p => p.PaymentId == paymentId)
            ?? throw new InvalidOperationException("Payment not found.");
        var proposal = await _proposals.GetByIdAsync(payment.ProposalId)
            ?? throw new InvalidOperationException("Proposal not found.");

        if (payment.Status == "SUCCESS")
            throw new InvalidOperationException("Payment has already succeeded.");
        if (payment.Attempts.Count >= MaxAttempts)
            throw new InvalidOperationException("Maximum payment attempts exceeded. Please start a new payment.");

        return await ProcessAttemptAsync(payment, proposal, request.SimulateResult);
    }

    private async Task<PaymentResponse> ProcessAttemptAsync(Payment payment, Proposal proposal, string? simulateResult)
    {
        var attemptCount = await _attempts.Query().CountAsync(a => a.PaymentId == payment.PaymentId);
        var attemptNumber = attemptCount + 1;

        var outcome = (simulateResult ?? "SUCCESS").ToUpperInvariant();
        if (outcome is not ("SUCCESS" or "FAILED" or "TIMEOUT")) outcome = "SUCCESS";

        var attempt = new PaymentAttempt
        {
            AttemptId = Guid.NewGuid(),
            PaymentId = payment.PaymentId,
            AttemptNumber = attemptNumber,
            Status = outcome,
            FailureReason = outcome == "SUCCESS" ? null : $"Simulated {outcome.ToLowerInvariant()}",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _attempts.AddAsync(attempt);

        payment.Status = outcome;
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation("Payment {PaymentId} attempt #{Attempt} => {Outcome}", payment.PaymentId, attemptNumber, outcome);

        Guid? issuedPolicyId = null;
        string? issuedPolicyNumber = null;

        var customer = await _customers.GetByIdAsync(proposal.CustomerId);

        if (outcome == "SUCCESS")
        {
            ProposalService.EnsureTransitionAllowed(proposal.Status, ProposalStatus.Completed);
            proposal.Status = ProposalStatus.Completed;
            proposal.UpdatedAt = DateTimeOffset.UtcNow;
            await _attempts.SaveChangesAsync();

            if (customer != null)
            {
                await _notifications.SendAsync("PAYMENT_SUCCESS", customer.CustomerId, customer.Email, new Dictionary<string, string?>
                {
                    ["firstName"] = customer.FirstName,
                    ["amount"] = payment.Amount.ToString("0.00"),
                    ["proposalId"] = proposal.ProposalId.ToString()
                });
            }

            var policy = await _policyService.IssueAsync(proposal.ProposalId);
            issuedPolicyId = policy.PolicyId;
            issuedPolicyNumber = policy.PolicyNumber;
        }
        else if (attemptNumber >= MaxAttempts)
        {
            ProposalService.EnsureTransitionAllowed(proposal.Status, ProposalStatus.PaymentFailed);
            proposal.Status = ProposalStatus.PaymentFailed;
            proposal.UpdatedAt = DateTimeOffset.UtcNow;
            await _attempts.SaveChangesAsync();

            if (customer != null)
            {
                await _notifications.SendAsync("PAYMENT_FAILED", customer.CustomerId, customer.Email, new Dictionary<string, string?>
                {
                    ["firstName"] = customer.FirstName,
                    ["amount"] = payment.Amount.ToString("0.00"),
                    ["proposalId"] = proposal.ProposalId.ToString()
                });
            }
        }
        else
        {
            await _attempts.SaveChangesAsync();
        }

        return new PaymentResponse
        {
            PaymentId = payment.PaymentId,
            ProposalId = payment.ProposalId,
            Amount = payment.Amount,
            Mode = payment.Mode,
            Status = payment.Status,
            AttemptCount = attemptNumber,
            PolicyId = issuedPolicyId,
            PolicyNumber = issuedPolicyNumber
        };
    }

    private async Task<PaymentResponse> MapToResponseAsync(Payment payment)
    {
        return new PaymentResponse
        {
            PaymentId = payment.PaymentId,
            ProposalId = payment.ProposalId,
            Amount = payment.Amount,
            Mode = payment.Mode,
            Status = payment.Status,
            AttemptCount = payment.Attempts.Count
        };
    }
}
