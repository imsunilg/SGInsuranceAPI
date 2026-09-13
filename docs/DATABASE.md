# Database

## Source of truth

The actual database (schema + seed data) already exists: PostgreSQL 16,
database `SGInsurance`, schema `"SGInsurance"` (mixed-case, always quoted),
created and seeded by the separate `SGInsuranceDB` repository's scripts
`001_create_schema.sql` through `007_seed_demo_users_and_data.sql`. This API
repo **never runs those scripts and never modifies that database** - it only
reads/writes rows through EF Core once the connection string points at it.

Connection string (`src/SGInsurance.Api/appsettings.json`,
`ConnectionStrings:DefaultConnection`):

```
Host=localhost;Port=5432;Database=SGInsurance;Username=postgres;Password=284228
```

## Intentional duplication: SQL scripts vs EF Core model

Both the `SGInsuranceDB` SQL scripts and this repo's EF Core model
(`SGInsurance.Domain` entities + `SGInsurance.Infrastructure/Persistence/Configurations`)
describe the *same* 22 tables. This is deliberate duplication for learning
purposes: it shows both "raw SQL DDL" and "Fluent API/code-first" ways of
describing a schema, and lets you compare them side by side.

Every entity in `SGInsurance.Domain.Entities` has a matching
`IEntityTypeConfiguration<T>` in `SGInsurance.Infrastructure/Persistence/Configurations`
that maps it onto the exact table/column names from the SQL scripts (snake_case
columns, `jsonb` columns kept as raw JSON strings on the C# side and parsed
on demand with `System.Text.Json`, composite keys for join/junction tables
like `user_roles`, `product_addons`, `quote_addons`).

## Reconciling "schema already exists" with "generate an EF Core migration"

We took the approach the brief calls out as simplest:

1. We ran `dotnet ef migrations add InitialCreate` (from the repo root:
   `dotnet ef migrations add InitialCreate --project src/SGInsurance.Infrastructure --startup-project src/SGInsurance.Api --output-dir Persistence/Migrations`).
   This generates `src/SGInsurance.Infrastructure/Persistence/Migrations/*InitialCreate*.cs`
   purely as **documentation/tooling proof** that the EF Core model is
   internally consistent and could stand up a fresh database from scratch.
2. We **did not** run `dotnet ef database update` and did not insert a row
   into `__EFMigrationsHistory` - the real SGInsurance database already has
   the schema and 8+ LOBs / 25 products / demo users / 8 seeded policies, so
   applying (or "faking apply" of) the migration is unnecessary and risks
   confusion about which system owns the schema.
3. Instead, we verified the EF Core model actually matches the live,
   already-seeded database with an integration test that has no in-memory
   fakes - `tests/SGInsurance.IntegrationTests/EfCoreSchemaTests.cs` opens a
   real `SGInsuranceDbContext` against the real connection string and
   asserts the expected seeded row counts (8 LOBs, 25 products, 79+ addons,
   25 premium rules, 8 notification templates, both demo users, 8+ policies).
   This test passes, which is the actual proof the reconciliation worked -
   not just "the migration file compiles".

If this project were ever deployed against a brand-new empty database, the
generated `InitialCreate` migration is ready to run standard EF workflow
(`dotnet ef database update`) - it just wasn't needed here because
`SGInsuranceDB`'s scripts already did that job for the shared `SGInsurance`
database.

## Key JSON column shapes

- `product_master.config` / `premium_rules.config` - per-LOB rating knobs,
  see `docs/API.md` and `SGInsurance.Application/Rating/Strategies.cs`.
- `quotes.product_data` / `proposals.proposal_data` - the flexible per-LOB
  "form data" shape (vehicle details for MOTOR, members for HEALTH, etc.),
  read via `System.Text.Json.JsonElement` rather than being modeled as
  strongly-typed columns, so one generic pipeline handles all 8 LOBs.
