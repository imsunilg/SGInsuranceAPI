using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGInsurance.Application.Interfaces;
using SGInsurance.Application.Services;

namespace SGInsurance.Api.Controllers;

/// <summary>
/// Admin-only read (and a couple of write) endpoints over the raw tables, for the
/// admin back-office screens. Kept intentionally thin/generic - this is a learning
/// app, not a full CRUD framework.
/// </summary>
[Route("api/v1/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminController : ApiControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly IPremiumRuleRepository _premiumRules;
    private readonly IQuoteRepository _quotes;
    private readonly IProposalRepository _proposals;
    private readonly IPaymentRepository _payments;
    private readonly IPolicyRepository _policies;
    private readonly INotificationRepository _notifications;
    private readonly IAuditLogRepository _auditLogs;

    public AdminController(
        IAdminService adminService,
        ICustomerRepository customers,
        IProductRepository products,
        IPremiumRuleRepository premiumRules,
        IQuoteRepository quotes,
        IProposalRepository proposals,
        IPaymentRepository payments,
        IPolicyRepository policies,
        INotificationRepository notifications,
        IAuditLogRepository auditLogs)
    {
        _adminService = adminService;
        _customers = customers;
        _products = products;
        _premiumRules = premiumRules;
        _quotes = quotes;
        _proposals = proposals;
        _payments = payments;
        _policies = policies;
        _notifications = notifications;
        _auditLogs = auditLogs;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult> Dashboard() => Ok(await _adminService.GetDashboardAsync());

    [HttpGet("customers")]
    public async Task<ActionResult> Customers() => Ok(await _customers.Query().OrderByDescending(c => c.CreatedAt).ToListAsync());

    [HttpGet("products")]
    public async Task<ActionResult> Products() => Ok(await _products.Query().OrderBy(p => p.ProductCode).ToListAsync());

    [HttpGet("premium-rules")]
    public async Task<ActionResult> PremiumRules() => Ok(await _premiumRules.Query().OrderBy(r => r.ProductCode).ToListAsync());

    [HttpPut("premium-rules/{ruleId:guid}")]
    public async Task<ActionResult> UpdatePremiumRule(Guid ruleId, [FromBody] string config)
    {
        var rule = await _premiumRules.GetByIdAsync(ruleId);
        if (rule == null) return NotFound(Application.Common.ApiResponse<object?>.Fail("Premium rule not found."));
        rule.Config = config;
        await _premiumRules.SaveChangesAsync();
        return Ok<object?>(null, "Premium rule updated.");
    }

    [HttpGet("quotes")]
    public async Task<ActionResult> Quotes() => Ok(await _quotes.Query().OrderByDescending(q => q.CreatedAt).ToListAsync());

    [HttpGet("proposals")]
    public async Task<ActionResult> Proposals() => Ok(await _proposals.Query().OrderByDescending(p => p.CreatedAt).ToListAsync());

    [HttpGet("payments")]
    public async Task<ActionResult> Payments() => Ok(await _payments.Query().OrderByDescending(p => p.CreatedAt).ToListAsync());

    [HttpGet("policies")]
    public async Task<ActionResult> Policies() => Ok(await _policies.Query().OrderByDescending(p => p.IssuedAt).ToListAsync());

    [HttpGet("notifications")]
    public async Task<ActionResult> Notifications() => Ok(await _notifications.Query().OrderByDescending(n => n.SentAt).Take(200).ToListAsync());

    [HttpGet("audit-logs")]
    public async Task<ActionResult> AuditLogs() => Ok(await _auditLogs.Query().OrderByDescending(a => a.CreatedAt).Take(200).ToListAsync());
}
