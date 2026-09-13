using Microsoft.EntityFrameworkCore;
using SGInsurance.Application.Interfaces;

namespace SGInsurance.Application.Services;

public interface IPolicyNumberGenerator
{
    Task<string> GenerateAsync(string lobCode, int year);
}

/// <summary>
/// Format: SG-{LOB}-{YYYY}-{sequence}, sequence per LOB per year. Queries the max
/// existing sequence for that LOB+year and adds one. This is a local single-user
/// learning app so a simple query-then-insert is an acceptable (not fully
/// concurrency-safe) approach - documented here rather than hidden.
/// </summary>
public class PolicyNumberGenerator : IPolicyNumberGenerator
{
    private readonly IPolicyRepository _policies;

    public PolicyNumberGenerator(IPolicyRepository policies)
    {
        _policies = policies;
    }

    public Task<string> GenerateAsync(string lobCode, int year)
    {
        var prefix = $"SG-{lobCode}-{year}-";
        // Synchronous LINQ-to-objects here (not ToListAsync) so this also works against
        // plain in-memory IQueryable stubs in unit tests, not just a real EF Core DbSet.
        var existing = _policies.Query()
            .Where(p => p.PolicyNumber.StartsWith(prefix))
            .Select(p => p.PolicyNumber)
            .ToList();

        var maxSeq = 0;
        foreach (var number in existing)
        {
            var tail = number[prefix.Length..];
            if (int.TryParse(tail, out var seq) && seq > maxSeq) maxSeq = seq;
        }

        var next = maxSeq + 1;
        return Task.FromResult($"{prefix}{next.ToString().PadLeft(6, '0')}");
    }
}
