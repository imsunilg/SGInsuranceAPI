using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SGInsurance.Application.Interfaces;
using SGInsurance.Application.Rating;
using SGInsurance.Application.Services;

namespace SGInsurance.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Rating strategies - one per LOB, resolved through the factory by rating_strategy_key.
        services.AddScoped<IRatingStrategy, MotorRatingStrategy>();
        services.AddScoped<IRatingStrategy, HealthRatingStrategy>();
        services.AddScoped<IRatingStrategy, TravelRatingStrategy>();
        services.AddScoped<IRatingStrategy, HomeRatingStrategy>();
        services.AddScoped<IRatingStrategy, PersonalAccidentRatingStrategy>();
        services.AddScoped<IRatingStrategy, CommercialRatingStrategy>();
        services.AddScoped<IRatingStrategy, AgricultureRatingStrategy>();
        services.AddScoped<IRatingStrategy, CyberRatingStrategy>();
        services.AddScoped<IRatingStrategyFactory, RatingStrategyFactory>();
        services.AddScoped<IPremiumCalculationService, PremiumCalculationService>();

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IQuoteService, QuoteService>();
        services.AddScoped<IProposalService, ProposalService>();
        services.AddScoped<IPolicyNumberGenerator, PolicyNumberGenerator>();
        services.AddScoped<IPolicyService, PolicyService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<INotificationQueryService, NotificationQueryService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
