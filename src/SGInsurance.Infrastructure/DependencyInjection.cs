using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SGInsurance.Application.Interfaces;
using SGInsurance.Infrastructure.Persistence;
using SGInsurance.Infrastructure.Repositories;

namespace SGInsurance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<SGInsuranceDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", SGInsuranceDbContext.Schema)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ILobRepository, LobRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductAddonRepository, ProductAddonRepository>();
        services.AddScoped<IPremiumRuleRepository, PremiumRuleRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IProposalRepository, ProposalRepository>();
        services.AddScoped<IKycRepository, KycRepository>();
        services.AddScoped<IRiskVerificationRepository, RiskVerificationRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentAttemptRepository, PaymentAttemptRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IPolicyDocumentRepository, PolicyDocumentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();

        return services;
    }
}
