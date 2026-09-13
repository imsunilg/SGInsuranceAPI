using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SGInsurance.Infrastructure.Persistence;
using Xunit;

namespace SGInsurance.IntegrationTests;

/// <summary>
/// End-to-end proof that the same generalized pipeline (register -> login ->
/// quote -> proposal -> KYC -> risk verification -> payment -> policy issuance)
/// works across different LOBs/products, not just MOTOR. Runs against the real
/// local Postgres "taskflow" database; each run registers a brand-new customer
/// (unique email/mobile) so it's safe to run repeatedly without cleanup.
/// </summary>
public class FullLifecycleFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FullLifecycleFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public static IEnumerable<object[]> LobCases()
    {
        yield return new object[]
        {
            "PRIVATE_CAR", "MOTOR",
            JsonSerializer.SerializeToElement(new
            {
                regNumber = "KA01AB1234",
                make = "MARUTI",
                model = "SWIFT",
                year = 2021,
                fuelType = "PETROL",
                seatingCapacity = 5,
                sumInsured = 500000,
                ncbPercent = 1
            })
        };
        yield return new object[]
        {
            "INDIVIDUAL_HEALTH", "HEALTH",
            JsonSerializer.SerializeToElement(new
            {
                members = new[] { new { name = "Self", dob = "1990-01-01", relationship = "SELF" } },
                sumInsured = 500000,
                preExistingConditions = false,
                cityTier = "TIER1"
            })
        };
        yield return new object[]
        {
            "HOME_CONTENTS", "HOME",
            JsonSerializer.SerializeToElement(new
            {
                propertyAddress = "123 Test Street",
                constructionType = "RCC",
                builtUpAreaSqFt = 1000,
                propertyAge = 5,
                sumInsuredStructure = 0,
                sumInsuredContents = 300000
            })
        };
    }

    [Theory]
    [MemberData(nameof(LobCases))]
    public async Task Full_flow_from_registration_to_policy_issuance(string productCode, string lobCode, JsonElement productData)
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"itest-{unique}@example.com";
        var mobile = "9" + Random.Shared.NextInt64(100000000, 999999999);

        // 1. Register
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Passw0rd1",
            firstName = "Integration",
            lastName = "Test",
            mobile
        });
        registerResponse.EnsureSuccessStatusCode();

        // 2. Login
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd1" });
        loginResponse.EnsureSuccessStatusCode();
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginBody.GetProperty("data").GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Look up the customer_id created alongside the user (not exposed on AuthResponse).
        var customerId = await GetCustomerIdByEmailAsync(email);

        // 3. Create quote
        var quoteResponse = await client.PostAsJsonAsync("/api/v1/quotes", new
        {
            customerId,
            productCode,
            lobCode,
            productData,
            addOns = Array.Empty<string>()
        });
        var quoteBody = await quoteResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(quoteResponse.IsSuccessStatusCode, quoteBody.ToString());
        var quoteId = quoteBody.GetProperty("data").GetProperty("quoteId").GetGuid();
        Assert.True(quoteBody.GetProperty("data").GetProperty("totalPremium").GetDecimal() > 0);

        // 4. Create proposal
        var proposalResponse = await client.PostAsJsonAsync("/api/v1/proposals", new
        {
            quoteId,
            proposalData = JsonSerializer.SerializeToElement(new { note = "integration test" }),
            nomineeData = JsonSerializer.SerializeToElement(new { name = "Nominee", relationship = "SPOUSE" })
        });
        var proposalBody = await proposalResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(proposalResponse.IsSuccessStatusCode, proposalBody.ToString());
        var proposalId = proposalBody.GetProperty("data").GetProperty("proposalId").GetGuid();

        // 5. Submit proposal -> KYC_PENDING
        var submitResponse = await client.PostAsync($"/api/v1/proposals/{proposalId}/submit", null);
        submitResponse.EnsureSuccessStatusCode();

        // 6. KYC verify with a valid PAN -> VERIFICATION_PENDING
        var kycResponse = await client.PostAsJsonAsync($"/api/v1/proposals/{proposalId}/kyc/verify", new
        {
            pan = "ABCPQ1234R",
            name = "Integration Test",
            dob = "1990-01-01",
            address = "Test Address"
        });
        var kycBody = await kycResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(kycResponse.IsSuccessStatusCode, kycBody.ToString());
        Assert.Equal("VERIFIED", kycBody.GetProperty("data").GetProperty("result").GetString());

        // 7. Risk verification -> PAYMENT_PENDING
        var verificationType = lobCode switch
        {
            "MOTOR" => "VEHICLE",
            "HEALTH" => "HEALTH_DECLARATION",
            "HOME" => "PROPERTY",
            _ => "HEALTH_DECLARATION"
        };
        var verifyResponse = await client.PostAsJsonAsync($"/api/v1/proposals/{proposalId}/verification/verify", new
        {
            verificationType,
            verificationData = JsonSerializer.SerializeToElement(new { forceFail = false })
        });
        var verifyBody = await verifyResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(verifyResponse.IsSuccessStatusCode, verifyBody.ToString());
        Assert.Equal("PASSED", verifyBody.GetProperty("data").GetProperty("status").GetString());

        // 8. Payment (simulate success) -> policy issued, proposal COMPLETED
        var totalPremium = quoteBody.GetProperty("data").GetProperty("totalPremium").GetDecimal();
        var paymentResponse = await client.PostAsJsonAsync("/api/v1/payments", new
        {
            proposalId,
            amount = totalPremium,
            mode = "UPI",
            simulateResult = "SUCCESS"
        });
        var paymentBody = await paymentResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(paymentResponse.IsSuccessStatusCode, paymentBody.ToString());
        Assert.Equal("SUCCESS", paymentBody.GetProperty("data").GetProperty("status").GetString());
        var policyNumber = paymentBody.GetProperty("data").GetProperty("policyNumber").GetString();
        Assert.False(string.IsNullOrWhiteSpace(policyNumber));
        Assert.StartsWith($"SG-{lobCode}-", policyNumber);

        // 9. Verify the policy is queryable for this customer.
        var policiesResponse = await client.GetAsync($"/api/v1/policies/customer/{customerId}");
        policiesResponse.EnsureSuccessStatusCode();
        var policiesBody = await policiesResponse.Content.ReadFromJsonAsync<JsonElement>();
        var policies = policiesBody.GetProperty("data").EnumerateArray().ToList();
        Assert.Contains(policies, p => p.GetProperty("policyNumber").GetString() == policyNumber);
    }

    private static async Task<Guid> GetCustomerIdByEmailAsync(string email)
    {
        var options = new DbContextOptionsBuilder<SGInsuranceDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=taskflow;Username=postgres;Password=284228")
            .Options;
        await using var ctx = new SGInsuranceDbContext(options);
        var customer = await ctx.Customers.FirstAsync(c => c.Email == email);
        return customer.CustomerId;
    }
}
