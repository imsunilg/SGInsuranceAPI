using Microsoft.EntityFrameworkCore;
using SGInsurance.Domain.Entities;

namespace SGInsurance.Infrastructure.Persistence;

public class SGInsuranceDbContext : DbContext
{
    public const string Schema = "SGInsurance";

    public SGInsuranceDbContext(DbContextOptions<SGInsuranceDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<LobMaster> LobMasters => Set<LobMaster>();
    public DbSet<ProductMaster> Products => Set<ProductMaster>();
    public DbSet<ProductAddon> ProductAddons => Set<ProductAddon>();
    public DbSet<PremiumRule> PremiumRules => Set<PremiumRule>();

    public DbSet<VehicleMake> VehicleMakes => Set<VehicleMake>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    public DbSet<FuelType> FuelTypes => Set<FuelType>();
    public DbSet<PaymentMode> PaymentModes => Set<PaymentMode>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteAddon> QuoteAddons => Set<QuoteAddon>();
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<KycVerification> KycVerifications => Set<KycVerification>();
    public DbSet<RiskVerification> RiskVerifications => Set<RiskVerification>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<PolicyDocument> PolicyDocuments => Set<PolicyDocument>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SGInsuranceDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
