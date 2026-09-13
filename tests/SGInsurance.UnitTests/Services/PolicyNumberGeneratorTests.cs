using Moq;
using SGInsurance.Application.Interfaces;
using SGInsurance.Application.Services;
using SGInsurance.Domain.Entities;
using Xunit;

namespace SGInsurance.UnitTests.Services;

public class PolicyNumberGeneratorTests
{
    [Fact]
    public async Task Generates_first_number_for_lob_year_with_no_existing_policies()
    {
        var repo = new Mock<IPolicyRepository>();
        repo.Setup(r => r.Query()).Returns(new List<Policy>().AsQueryable());

        var generator = new PolicyNumberGenerator(repo.Object);
        var number = await generator.GenerateAsync("MOTOR", 2026);

        Assert.Equal("SG-MOTOR-2026-000001", number);
    }

    [Fact]
    public async Task Increments_sequence_per_lob_per_year()
    {
        var existing = new List<Policy>
        {
            new() { PolicyNumber = "SG-MOTOR-2026-000001" },
            new() { PolicyNumber = "SG-MOTOR-2026-000002" },
            new() { PolicyNumber = "SG-HEALTH-2026-000001" }, // different LOB, must not affect MOTOR sequence
            new() { PolicyNumber = "SG-MOTOR-2025-000009" }   // different year, must not affect 2026 sequence
        };
        var repo = new Mock<IPolicyRepository>();
        repo.Setup(r => r.Query()).Returns(existing.AsQueryable());

        var generator = new PolicyNumberGenerator(repo.Object);
        var number = await generator.GenerateAsync("MOTOR", 2026);

        Assert.Equal("SG-MOTOR-2026-000003", number);
    }
}
