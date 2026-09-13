using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SGInsurance.Application.DTOs;
using SGInsurance.Application.Interfaces;
using SGInsurance.Domain.Entities;

namespace SGInsurance.Application.Services;

public interface IPolicyService
{
    Task<Policy> IssueAsync(Guid proposalId);
    Task<List<PolicyResponse>> GetAllAsync();
    Task<PolicyResponse?> GetByIdAsync(Guid policyId);
    Task<List<PolicyResponse>> GetByCustomerAsync(Guid customerId);
    Task<string> GenerateDocumentAsync(Guid policyId);
}

public class PolicyService : IPolicyService
{
    private readonly IPolicyRepository _policies;
    private readonly IPolicyDocumentRepository _documents;
    private readonly IProposalRepository _proposals;
    private readonly IQuoteRepository _quotes;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly IPolicyNumberGenerator _numberGenerator;
    private readonly INotificationService _notifications;
    private readonly IConfiguration _config;
    private readonly ILogger<PolicyService> _logger;

    public PolicyService(
        IPolicyRepository policies,
        IPolicyDocumentRepository documents,
        IProposalRepository proposals,
        IQuoteRepository quotes,
        ICustomerRepository customers,
        IProductRepository products,
        IPolicyNumberGenerator numberGenerator,
        INotificationService notifications,
        IConfiguration config,
        ILogger<PolicyService> logger)
    {
        _policies = policies;
        _documents = documents;
        _proposals = proposals;
        _quotes = quotes;
        _customers = customers;
        _products = products;
        _numberGenerator = numberGenerator;
        _notifications = notifications;
        _config = config;
        _logger = logger;
    }

    public async Task<Policy> IssueAsync(Guid proposalId)
    {
        var proposal = await _proposals.GetByIdAsync(proposalId)
            ?? throw new InvalidOperationException("Proposal not found.");
        var quote = await _quotes.GetByIdAsync(proposal.QuoteId)
            ?? throw new InvalidOperationException("Quote not found.");

        var year = DateTime.UtcNow.Year;
        var policyNumber = await _numberGenerator.GenerateAsync(quote.LobCode, year);

        var policy = new Policy
        {
            PolicyId = Guid.NewGuid(),
            PolicyNumber = policyNumber,
            ProposalId = proposalId,
            CustomerId = proposal.CustomerId,
            ProductCode = quote.ProductCode,
            LobCode = quote.LobCode,
            CoverageData = quote.ProductData,
            SumInsured = quote.SumInsured,
            TotalPremium = quote.TotalPremium,
            GstAmount = quote.GstAmount,
            RiskStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            RiskEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            Status = "ACTIVE",
            IssuedAt = DateTimeOffset.UtcNow
        };
        await _policies.AddAsync(policy);
        await _policies.SaveChangesAsync();

        _logger.LogInformation("Policy {PolicyNumber} issued for proposal {ProposalId}", policyNumber, proposalId);

        await GenerateDocumentAsync(policy.PolicyId);

        var customer = await _customers.GetByIdAsync(proposal.CustomerId);
        if (customer != null)
        {
            await _notifications.SendAsync("POLICY_ISSUED", customer.CustomerId, customer.Email, new Dictionary<string, string?>
            {
                ["firstName"] = customer.FirstName,
                ["policyNumber"] = policy.PolicyNumber
            });
        }

        return policy;
    }

    public async Task<List<PolicyResponse>> GetAllAsync() =>
        (await _policies.Query().OrderByDescending(p => p.IssuedAt).ToListAsync()).Select(MapToResponse).ToList();

    public async Task<PolicyResponse?> GetByIdAsync(Guid policyId)
    {
        var policy = await _policies.GetByIdAsync(policyId);
        return policy == null ? null : MapToResponse(policy);
    }

    public async Task<List<PolicyResponse>> GetByCustomerAsync(Guid customerId) =>
        (await _policies.Query().Where(p => p.CustomerId == customerId).OrderByDescending(p => p.IssuedAt).ToListAsync())
            .Select(MapToResponse).ToList();

    public async Task<string> GenerateDocumentAsync(Guid policyId)
    {
        var policy = await _policies.GetByIdAsync(policyId)
            ?? throw new InvalidOperationException("Policy not found.");
        var customer = await _customers.GetByIdAsync(policy.CustomerId);
        var product = await _products.GetByIdAsync(policy.ProductCode);

        var root = _config["GeneratedDocuments:RootPath"] ?? "generated-documents";
        Directory.CreateDirectory(root);
        var fileName = $"{policy.PolicyNumber}.html";
        var filePath = Path.Combine(root, fileName);

        var html = $@"<html><head><title>Policy {policy.PolicyNumber}</title></head><body>
<h1>SGInsurance Policy Document</h1>
<p><b>Policy Number:</b> {policy.PolicyNumber}</p>
<p><b>Policyholder:</b> {customer?.FirstName} {customer?.LastName}</p>
<p><b>Product:</b> {product?.ProductName} ({policy.LobCode})</p>
<p><b>Sum Insured:</b> {policy.SumInsured:0.00}</p>
<p><b>Total Premium (incl. GST):</b> {policy.TotalPremium + policy.GstAmount:0.00}</p>
<p><b>Risk Period:</b> {policy.RiskStartDate:yyyy-MM-dd} to {policy.RiskEndDate:yyyy-MM-dd}</p>
<p><b>Status:</b> {policy.Status}</p>
<p><i>This is a system-generated document for a local learning application. No real insurance cover is provided.</i></p>
</body></html>";
        File.WriteAllText(filePath, html);

        var doc = new PolicyDocument
        {
            DocId = Guid.NewGuid(),
            PolicyId = policyId,
            FilePath = filePath,
            Version = 1,
            GeneratedAt = DateTimeOffset.UtcNow
        };
        await _documents.AddAsync(doc);
        await _documents.SaveChangesAsync();

        return filePath;
    }

    private static PolicyResponse MapToResponse(Policy p) => new()
    {
        PolicyId = p.PolicyId,
        PolicyNumber = p.PolicyNumber,
        CustomerId = p.CustomerId,
        ProductCode = p.ProductCode,
        LobCode = p.LobCode,
        SumInsured = p.SumInsured,
        TotalPremium = p.TotalPremium,
        GstAmount = p.GstAmount,
        RiskStartDate = p.RiskStartDate,
        RiskEndDate = p.RiskEndDate,
        Status = p.Status,
        IssuedAt = p.IssuedAt
    };
}
