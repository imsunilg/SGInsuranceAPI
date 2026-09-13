using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace SGInsurance.IntegrationTests;

/// <summary>
/// Boots the real SGInsurance.Api Program.cs (top-level statements, resolved via
/// the same HostFactoryResolver mechanism `dotnet ef` uses) against the real local
/// Postgres "taskflow" database - there is no in-memory/test double for the DB in
/// this learning app, by design (see docs/DATABASE.md).
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=taskflow;Username=postgres;Password=284228",
                ["GeneratedDocuments:RootPath"] = Path.Combine(Path.GetTempPath(), "sginsurance-test-documents")
            });
        });
    }
}
