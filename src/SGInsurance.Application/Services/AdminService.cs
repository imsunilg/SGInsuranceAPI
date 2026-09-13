using Microsoft.EntityFrameworkCore;
using SGInsurance.Application.DTOs;
using SGInsurance.Application.Interfaces;

namespace SGInsurance.Application.Services;

public interface IAdminService
{
    Task<AdminDashboardResponse> GetDashboardAsync();
}

public class AdminService : IAdminService
{
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly IQuoteRepository _quotes;
    private readonly IProposalRepository _proposals;
    private readonly IPaymentRepository _payments;
    private readonly IPolicyRepository _policies;

    public AdminService(
        ICustomerRepository customers,
        IProductRepository products,
        IQuoteRepository quotes,
        IProposalRepository proposals,
        IPaymentRepository payments,
        IPolicyRepository policies)
    {
        _customers = customers;
        _products = products;
        _quotes = quotes;
        _proposals = proposals;
        _payments = payments;
        _policies = policies;
    }

    public async Task<AdminDashboardResponse> GetDashboardAsync() => new()
    {
        Customers = await _customers.Query().CountAsync(),
        Products = await _products.Query().CountAsync(),
        Quotes = await _quotes.Query().CountAsync(),
        Proposals = await _proposals.Query().CountAsync(),
        Payments = await _payments.Query().CountAsync(),
        Policies = await _policies.Query().CountAsync(),
        TotalPremium = await _policies.Query().SumAsync(p => (decimal?)(p.TotalPremium + p.GstAmount)) ?? 0
    };
}

public interface INotificationQueryService
{
    Task<List<NotificationDto>> GetByCustomerAsync(Guid customerId);
}

public class NotificationQueryService : INotificationQueryService
{
    private readonly INotificationRepository _notifications;

    public NotificationQueryService(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<List<NotificationDto>> GetByCustomerAsync(Guid customerId) =>
        (await _notifications.Query().Where(n => n.CustomerId == customerId).OrderByDescending(n => n.SentAt).ToListAsync())
        .Select(n => new NotificationDto
        {
            NotificationId = n.NotificationId,
            TemplateCode = n.TemplateCode,
            Channel = n.Channel,
            Recipient = n.Recipient,
            Status = n.Status,
            SentAt = n.SentAt
        }).ToList();
}
