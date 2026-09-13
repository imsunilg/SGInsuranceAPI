using Microsoft.EntityFrameworkCore;
using SGInsurance.Infrastructure.Persistence;
using Xunit;

namespace SGInsurance.IntegrationTests;

/// <summary>
/// Verifies EF Core's model (built from Fluent API configurations, independent
/// of the SGInsuranceDB SQL scripts) can query the real, already-seeded
/// "SGInsurance" schema in the local "SGInsurance" Postgres database. This is the
/// Phase 9 reconciliation check: we did NOT run `dotnet ef database update`
/// (the schema+data already exist), we just proved EF's mapping matches it.
/// </summary>
public class EfCoreSchemaTests
{
    private static SGInsuranceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SGInsuranceDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=SGInsurance;Username=postgres;Password=284228")
            .Options;
        return new SGInsuranceDbContext(options);
    }

    [Fact]
    public async Task Can_query_seeded_lobs_and_products()
    {
        await using var ctx = CreateContext();

        var lobCount = await ctx.LobMasters.CountAsync();
        var productCount = await ctx.Products.CountAsync();
        var addonCount = await ctx.ProductAddons.CountAsync();
        var ruleCount = await ctx.PremiumRules.CountAsync();
        var templateCount = await ctx.NotificationTemplates.CountAsync();

        Assert.Equal(8, lobCount);
        Assert.Equal(25, productCount);
        Assert.True(addonCount >= 79);
        Assert.Equal(25, ruleCount);
        Assert.Equal(8, templateCount);
    }

    [Fact]
    public async Task Demo_users_and_policies_are_seeded()
    {
        await using var ctx = CreateContext();

        var adminExists = await ctx.Users.AnyAsync(u => u.Email == "admin@sginsurance.com");
        var customerExists = await ctx.Users.AnyAsync(u => u.Email == "customer@sginsurance.com");
        var policyCount = await ctx.Policies.CountAsync();

        Assert.True(adminExists);
        Assert.True(customerExists);
        Assert.True(policyCount >= 8);
    }
}
