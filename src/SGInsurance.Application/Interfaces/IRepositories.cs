using SGInsurance.Domain.Entities;

namespace SGInsurance.Application.Interfaces;

/// <summary>
/// Minimal generic repository abstraction. Defined here (Application) and
/// implemented in Infrastructure, so services depend only on the interface.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(params object[] keyValues);
    IQueryable<T> Query();
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync();
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByCodeAsync(string code);
}

public interface ILobRepository : IRepository<LobMaster> { }
public interface IProductRepository : IRepository<ProductMaster> { }
public interface IProductAddonRepository : IRepository<ProductAddon> { }
public interface IPremiumRuleRepository : IRepository<PremiumRule>
{
    Task<PremiumRule?> GetActiveForProductAsync(string productCode);
}
public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByUserIdAsync(Guid userId);
}
public interface IQuoteRepository : IRepository<Quote> { }
public interface IProposalRepository : IRepository<Proposal> { }
public interface IKycRepository : IRepository<KycVerification> { }
public interface IRiskVerificationRepository : IRepository<RiskVerification> { }
public interface IPaymentRepository : IRepository<Payment> { }
public interface IPaymentAttemptRepository : IRepository<PaymentAttempt> { }
public interface IPolicyRepository : IRepository<Policy> { }
public interface IPolicyDocumentRepository : IRepository<PolicyDocument> { }
public interface INotificationRepository : IRepository<Notification> { }
public interface IAuditLogRepository : IRepository<AuditLog> { }
public interface INotificationTemplateRepository : IRepository<NotificationTemplate>
{
    Task<NotificationTemplate?> GetByCodeAsync(string code);
}
