# Learning guide

A tour of the concepts used in this codebase, each with a real file in this
repo you can open and a short example. Written for someone comfortable with
C# basics but new to ASP.NET Core Web APIs.

## 1. Controllers, DI, service layer, repository pattern

**Controller** (`src/SGInsurance.Api/Controllers/QuotesController.cs`) - thin,
HTTP-only concerns. It never touches EF Core directly:

```csharp
[HttpPost]
public async Task<ActionResult> Create(CreateQuoteRequest request)
{
    await ValidateAsync(_validator, request);
    return Ok(await _quoteService.CreateAsync(request), "Quote created.");
}
```

**Dependency Injection (DI)** - the constructor above receives
`IQuoteService` and `IValidator<CreateQuoteRequest>` without `new`-ing them
up; ASP.NET Core's built-in container supplies them because they were
registered in `src/SGInsurance.Application/DependencyInjection.cs`:

```csharp
services.AddScoped<IQuoteService, QuoteService>();
```

`AddScoped` means "one instance per HTTP request" - important because
`QuoteService` holds a reference (indirectly, through repositories) to the
EF Core `DbContext`, which is not thread-safe and should not outlive a
request.

**Service layer** (`src/SGInsurance.Application/Services/QuoteService.cs`) -
the actual business logic: resolving sum insured, calling the rating engine,
persisting the quote, firing a notification. Controllers stay thin because
this is where the real work happens.

**Repository pattern** - `IQuoteRepository` is declared in
`src/SGInsurance.Application/Interfaces/IRepositories.cs` (so the service
layer only depends on an abstraction) and implemented against EF Core in
`src/SGInsurance.Infrastructure/Repositories/SpecificRepositories.cs`. This
means you could swap the database technology by only changing
`Infrastructure`, never `Application`.

## 2. EF Core

`src/SGInsurance.Infrastructure/Persistence/SGInsuranceDbContext.cs` is the
EF Core "unit of work" - one `DbSet<T>` per table, model configuration
delegated to `IEntityTypeConfiguration<T>` classes (see
`src/SGInsurance.Infrastructure/Persistence/Configurations/`) instead of
data annotations, which keeps `Domain` entities free of any EF Core
attributes:

```csharp
public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> b)
    {
        b.ToTable("quotes");
        b.Property(x => x.SumInsured).HasColumnName("sum_insured").HasColumnType("numeric(14,2)");
        // ...
    }
}
```

See `docs/DATABASE.md` for how this model was verified against the real,
already-seeded database instead of running a fresh migration against it.

## 3. PostgreSQL

Npgsql (`Npgsql.EntityFrameworkCore.PostgreSQL`) is the EF Core provider
used, configured in `src/SGInsurance.Infrastructure/DependencyInjection.cs`.
Postgres-specific things you'll see: the `"SGInsurance"` schema (mixed-case,
needs quoting - `modelBuilder.HasDefaultSchema("SGInsurance")` in
`SGInsuranceDbContext.OnModelCreating`), `jsonb` columns mapped as raw
strings (`HasColumnType("jsonb")`), and `pgcrypto`'s `bcrypt` password
hashes verified with `BCrypt.Net-Next` (binary-compatible format).

## 4. DTOs

`src/SGInsurance.Application/DTOs/QuoteDtos.cs` - plain classes that shape
what crosses the HTTP boundary, deliberately different from the `Quote`
domain entity (e.g. `QuoteResponse` flattens fields and adds a computed
`ProductName`). Never return domain entities directly from a controller -
it couples your API contract to your database schema.

## 5. Validation (FluentValidation)

`src/SGInsurance.Application/Validators/QuoteProposalValidators.cs`:

```csharp
public class CreateQuoteRequestValidator : AbstractValidator<CreateQuoteRequest>
{
    public CreateQuoteRequestValidator()
    {
        RuleFor(x => x.ProductCode).NotEmpty();
        RuleFor(x => x).Custom((req, ctx) =>
        {
            foreach (var error in LobProductDataRules.Validate(req.LobCode, req.ProductData))
                ctx.AddFailure(nameof(req.ProductData), error);
        });
    }
}
```

Controllers call `ValidateAsync(_validator, request)` (see
`ApiControllerBase.ValidateAsync` in
`src/SGInsurance.Api/Controllers/ApiControllerBase.cs`), which throws
FluentValidation's `ValidationException` on failure - caught centrally by
the exception middleware (next section) rather than handled per-action.

## 6. Exception handling middleware

`src/SGInsurance.Api/Middleware/ExceptionHandlingMiddleware.cs` wraps every
request; any exception thrown anywhere downstream (a controller, a service,
EF Core) lands here and is converted into the standard envelope with the
right status code:

```csharp
catch (Exception ex) { await HandleAsync(context, ex); }
```

This is a **middleware**, registered once in `Program.cs`
(`app.UseSGInsuranceExceptionHandling()`) - one cross-cutting concern
instead of try/catch blocks in every action.

## 7. Serilog logging

`Program.cs` configures Serilog before the host even builds
(`Log.Logger = new LoggerConfiguration()...`), writing to both console and
`logs/sginsurance-.log` (daily rolling). `app.UseSerilogRequestLogging()`
logs every HTTP request/response automatically; individual services also
log key business events, e.g. `QuoteService`:

```csharp
_logger.LogInformation("Quote {QuoteId} created for product {ProductCode} totalling {Total}", quote.QuoteId, request.ProductCode, quote.TotalPremium);
```

Structured logging like this (named placeholders, not string
concatenation) lets you query/filter logs by field later.

## 8. REST API design (versioning, envelope)

Every route starts with `/api/v1/` (see any controller's `[Route(...)]`
attribute) so a future breaking change could ship as `/api/v2/` alongside
it. Every response - success or failure - uses the same
`ApiResponse<T>` shape (`src/SGInsurance.Application/Common/ApiResponse.cs`),
so a frontend can write one generic response handler instead of one per
endpoint.

## 9. The strategy-pattern rating engine

`src/SGInsurance.Application/Rating/IRatingStrategy.cs`:

```csharp
public interface IRatingStrategy
{
    string Key { get; }
    Task<PremiumResult> CalculateAsync(RatingContext ctx);
}
```

Eight implementations (`src/SGInsurance.Application/Rating/Strategies.cs`),
one per LOB, each reading its own numeric knobs out of the LOB's
`premium_rules.config` JSON rather than hardcoding rates. A factory
(`RatingStrategyFactory`) picks the right one at runtime by
`product_master.rating_strategy_key`:

```csharp
services.AddScoped<IRatingStrategy, MotorRatingStrategy>();
// ...seven more AddScoped<IRatingStrategy, ...>() lines...
services.AddScoped<IRatingStrategyFactory, RatingStrategyFactory>();
```

This is the Strategy design pattern: the *algorithm* (how to price a
policy) is swapped out based on data (the LOB), without a giant
`if/else`/`switch` anywhere in the calling code
(`PremiumCalculationService.CalculateAsync`). Adding a 9th LOB later means
adding one new class and one new DI registration line - nothing else
changes.

## 10. The general insurance business flow

See `docs/BUSINESS-FLOW.md` for the full
Quote -> Proposal -> KYC -> Verification -> Payment -> Policy -> Document ->
Notification -> Audit walkthrough, and how the same code path
(`ProposalService`, `PaymentService`, `PolicyService`) is shared across all
8 LOBs / 25 products - proven by
`tests/SGInsurance.IntegrationTests/FullLifecycleFlowTests.cs` running the
identical flow end-to-end for a MOTOR, a HEALTH, and a HOME product.
