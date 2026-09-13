# SGInsurance API - Architecture

A single ASP.NET Core Web API (no microservices, no message brokers, no
containers) built as a "Clean Architecture" / layered application, running
entirely on a local machine against a local PostgreSQL database.

## Deviation from spec

The task brief said ".NET 8". The only SDK installed in this environment is
**.NET 9 (9.0.305)** with no .NET 8 SDK available, so every project targets
`net9.0` instead. This is a purely mechanical substitution - nothing in the
code relies on .NET 9-only features.

## Layers

```
┌─────────────────────────────────────────────────────────────────┐
│  SGInsurance.Api                                                 │
│  Controllers, Middleware, Program.cs (DI/JWT/Swagger/CORS/Serilog)│
│  - depends on Application (business calls) and Infrastructure     │
│    (DI wiring only, in Program.cs - controllers never touch it)   │
└───────────────┬───────────────────────────────┬──────────────────┘
                │                                │
                ▼                                ▼
┌───────────────────────────────┐   ┌────────────────────────────────┐
│  SGInsurance.Application       │   │  SGInsurance.Infrastructure     │
│  DTOs, service interfaces +    │◄──┤  EF Core DbContext,             │
│  implementations, FluentValid- │   │  IEntityTypeConfiguration<T>,   │
│  ation validators, the rating  │   │  Repository<T> implementations  │
│  engine (IRatingStrategy x 8)  │   │                                  │
└───────────────┬─────────────────┘  └────────────────┬─────────────────┘
                │                                       │
                ▼                                       ▼
┌─────────────────────────────────────────────────────────────────┐
│  SGInsurance.Domain                                               │
│  Plain entities + status-constant classes. Zero dependency on     │
│  EF Core, ASP.NET, or any other layer.                            │
└─────────────────────────────────────────────────────────────────┘
```

Reference graph (matches the brief exactly):
`Api -> Application`, `Api -> Infrastructure` (DI wiring only, in
`Program.cs`/`DependencyInjection.cs`), `Infrastructure -> Application`,
`Infrastructure -> Domain`, `Application -> Domain`.

Controllers are thin: they validate the request (FluentValidation), call an
Application service, and wrap the result in the standard envelope
(`SGInsurance.Application.Common.ApiResponse<T>`). Services call repository
*interfaces* (`SGInsurance.Application.Interfaces.IRepository<T>` and
LOB-specific extensions of it) - the interfaces live in Application, their
EF Core-backed implementations live in Infrastructure
(`SGInsurance.Infrastructure.Repositories`), so Application never references
EF Core types directly in its public contracts (it does take a dependency on
the `Microsoft.EntityFrameworkCore` *package* for `IQueryable` LINQ operators
in a few services - a pragmatic trade-off for a small learning app rather
than a fully abstracted "repository never leaks IQueryable" design).

## Cross-cutting pieces

- **Rating engine** (`SGInsurance.Application/Rating`): `IRatingStrategy`,
  one implementation per LOB, resolved by `IRatingStrategyFactory` using
  `product_master.rating_strategy_key`. See LEARNING-GUIDE.md for a full
  walkthrough.
- **Notifications** (`SGInsurance.Application/Services/NotificationService.cs`):
  a dummy service that renders `notification_templates` rows, writes a
  `notifications` + `audit_logs` row, and drops an HTML file under
  `generated-documents/emails/`.
- **Exception handling** (`SGInsurance.Api/Middleware/ExceptionHandlingMiddleware.cs`):
  converts any unhandled exception (including `FluentValidation.ValidationException`)
  into the standard envelope with an appropriate HTTP status code.
- **Logging**: Serilog, console + daily rolling file sink under `logs/`,
  plus `UseSerilogRequestLogging()` for HTTP access logs and explicit
  `ILogger` calls for key business events (quote created, proposal
  submitted, KYC verified, payment attempted, policy issued).

## Why no microservices/Kafka/Docker/etc.

Per the brief, this is intentionally a single local Web API for a learning
project. All "distributed system" technology (Kafka, Kong, Kubernetes,
Redis, RabbitMQ/MassTransit, cloud services) is explicitly out of scope.
