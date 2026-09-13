using Microsoft.EntityFrameworkCore;
using SGInsurance.Application.Interfaces;
using SGInsurance.Domain.Entities;
using SGInsurance.Infrastructure.Persistence;

namespace SGInsurance.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(SGInsuranceDbContext ctx) : base(ctx) { }
    public Task<User?> GetByEmailAsync(string email) =>
        Set.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Email == email);
}

public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(SGInsuranceDbContext ctx) : base(ctx) { }
    public Task<Role?> GetByCodeAsync(string code) => Set.FirstOrDefaultAsync(r => r.RoleCode == code);
}

public class LobRepository : Repository<LobMaster>, ILobRepository
{
    public LobRepository(SGInsuranceDbContext ctx) : base(ctx) { }
}

public class ProductRepository : Repository<ProductMaster>, IProductRepository
{
    public ProductRepository(SGInsuranceDbContext ctx) : base(ctx) { }
}

public class ProductAddonRepository : Repository<ProductAddon>, IProductAddonRepository
{
    public ProductAddonRepository(SGInsuranceDbContext ctx) : base(ctx) { }
}

public class PremiumRuleRepository : Repository<PremiumRule>, IPremiumRuleRepository
{
    public PremiumRuleRepository(SGInsuranceDbContext ctx) : base(ctx) { }
    public Task<PremiumRule?> GetActiveForProductAsync(string productCode) =>
        Set.Where(r => r.ProductCode == productCode && (r.EffectiveTo == null || r.EffectiveTo >= DateOnly.FromDateTime(DateTime.UtcNow)))
           .OrderByDescending(r => r.EffectiveFrom).FirstOrDefaultAsync();
}

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(SGInsuranceDbContext ctx) : base(ctx) { }
    public Task<Customer?> GetByUserIdAsync(Guid userId) => Set.FirstOrDefaultAsync(c => c.UserId == userId);
}

public class QuoteRepository : Repository<Quote>, IQuoteRepository
{
    public QuoteRepository(SGInsuranceDbContext ctx) : base(ctx) { }
}

public class ProposalRepository : Repository<Proposal>, IProposalRepository
{
    public ProposalRepository(SGInsuranceDbContext ctx) : base(ctx) { }
}

public class KycRepository : Repository<KycVerification>, IKycRepository
{
    public KycRepository(SGInsuranceDbContext ctx) : base(ctx) { }
}

public class RiskVerificationRepository : Repository<RiskVerification>, IRiskVerificationRepository
{
    public RiskVerificationRepository(SGInsuranceDbContext ctx) : base(ctx) { }
}

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(SGInsuranceDbContext ctx) : base(ctx) { }
}

public class PaymentAttemptRepository : Repository<PaymentAttempt>, IPaymentAttemptRepository
{
    public PaymentAttemptRepository(SGInsuranceDbContext ctx) : base(ctx) { }
}

public class PolicyRepository : Repository<Policy>, IPolicyRepository
{
    public PolicyRepository(SGInsuranceDbContext ctx) : base(ctx) { }
}

public class PolicyDocumentRepository : Repository<PolicyDocument>, IPolicyDocumentRepository
{
    public PolicyDocumentRepository(SGInsuranceDbContext ctx) : base(ctx) { }
}

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(SGInsuranceDbContext ctx) : base(ctx) { }
}

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(SGInsuranceDbContext ctx) : base(ctx) { }
}

public class NotificationTemplateRepository : Repository<NotificationTemplate>, INotificationTemplateRepository
{
    public NotificationTemplateRepository(SGInsuranceDbContext ctx) : base(ctx) { }
    public Task<NotificationTemplate?> GetByCodeAsync(string code) => Set.FirstOrDefaultAsync(t => t.TemplateCode == code);
}
