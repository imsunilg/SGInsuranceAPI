# SGInsurance — Build Report

Full build of the SGInsurance multi-line insurance learning app, across three local git repositories, per the master build prompt. Completed end-to-end and verified working.

## 1. Repo structure

```
D:\Study\Angular\SGInsurance\
├── SGInsuranceDB\
│   ├── scripts\ (001_create_schema.sql … 999_reset_schema.sql)
│   ├── docs\ERD.md
│   └── README.md
├── SGInsuranceAPI\
│   ├── SGInsurance.sln
│   ├── src\
│   │   ├── SGInsurance.Api\           (Controllers, Middleware, Program.cs, Swagger/JWT config)
│   │   ├── SGInsurance.Application\   (DTOs, Interfaces, Services, Validators, Rating strategies)
│   │   ├── SGInsurance.Domain\        (Entities)
│   │   └── SGInsurance.Infrastructure\(DbContext, EF configs, Migrations, Repositories)
│   ├── tests\
│   │   ├── SGInsurance.UnitTests\
│   │   └── SGInsurance.IntegrationTests\
│   ├── templates\email\               (8 HTML notification templates)
│   ├── generated-documents\           (.gitkeep only — runtime output, gitignored)
│   └── docs\ (ARCHITECTURE, DATABASE, API, BUSINESS-FLOW, LEARNING-GUIDE, BUILD-REPORT).md
└── SGInsuranceWEB\
    └── sg-insurance\src\app\
        ├── core\ (guards, interceptors, models, services)
        ├── shared\ (components, pipes)
        ├── layout\ (header, sidebar, shell)
        └── features\ (auth, dashboard, products, buy, quotes, proposals, payments,
                        policies, notifications, profile, admin)
```

## 2. Database tables (with live seed row counts)

| Table | Rows | | Table | Rows |
|---|---|---|---|---|
| users | 14 | | payments | 23 |
| customers | 15 | | payment_attempts | 15 |
| lob_master | 8 | | policies | 23 |
| product_master | 25 | | policy_documents | 15 |
| product_addons | 79 | | notifications | 88 |
| premium_rules | 25 | | audit_logs | 88 |
| notification_templates | 8 | | roles | 3 |
| quotes | 24 | | user_roles | — |
| proposals | 23 | | vehicle_makes / models / fuel_types / payment_modes | seeded lookups |
| kyc_verifications | 23 | | risk_verifications | 23 |

(Counts include the original seed data plus the verification quotes/proposals/policies I created for MOTOR, HEALTH and HOME during end-to-end testing.)

## 3. API endpoints (all under `/api/v1`)

- **Auth**: `POST /auth/register`, `POST /auth/login`, `GET /auth/me`
- **Products**: `GET /lobs`, `GET /products?lobCode=`, `GET /products/{code}`, `POST/PUT/DELETE /products` (admin)
- **Quotes**: `POST /quotes`, `GET /quotes/{id}`, `GET /quotes/customer/{customerId}`, `POST /quotes/{id}/recalculate`, `DELETE /quotes/{id}`, `GET /quotes` (admin)
- **Proposals**: `POST /proposals`, `GET/PUT /proposals/{id}`, `POST /proposals/{id}/submit`
- **KYC**: `POST /proposals/{id}/kyc/verify`
- **Risk verification**: `POST /proposals/{id}/verification/verify`
- **Payments**: `POST /payments`, `GET /payments/{id}`, `POST /payments/{id}/retry`
- **Policies**: `GET /policies`, `GET /policies/{id}`, `GET /policies/customer/{customerId}`, `GET /policies/{id}/document`
- **Notifications**: `GET /notifications/customer/{customerId}`
- **Admin**: `GET /admin/dashboard`, `GET /admin/{customers,products,premium-rules,quotes,proposals,payments,policies,notifications,audit-logs}`, `PUT /admin/products/{code}`, `PUT /admin/premium-rules/{ruleId}`

Standard envelope on every response: `{ success, data, message, errors }`.

## 4. Angular routes

`/login`, `/register`, `/dashboard`, `/products`, `/products/:lobCode`, `/buy/:productCode`, `/quotes`, `/quotes/:id`, `/proposals`, `/proposals/:id`, `/payment/:proposalId`, `/policies`, `/policies/:id`, `/notifications`, `/profile`, `/admin`, `/admin/customers`, `/admin/products`, `/admin/quotes`, `/admin/proposals`, `/admin/payments`, `/admin/policies`, `/admin/audit` — all lazy-loaded standalone components, guarded per spec (`authGuard` / `adminGuard`).

## 5. Demo credentials

- Customer: `customer@sginsurance.com` / `Customer@123`
- Admin: `admin@sginsurance.com` / `Admin@123`

## 6. Starting PostgreSQL / verifying the schema

PostgreSQL 16 runs as a Windows service (`postgresql-x64-16`) — already running. To verify:

```
"C:\Program Files\PostgreSQL\16\bin\psql.exe" -h localhost -U postgres -d SGInsurance -c "\dt \"SGInsurance\".*"
```

To rebuild from scratch: run `SGInsuranceDB/scripts/999_reset_schema.sql` then `001` through `007` in order, via the same `psql.exe`.

## 7. Starting the API

```
cd SGInsuranceAPI
dotnet restore
dotnet build
dotnet test
dotnet run --project src/SGInsurance.Api --urls "http://localhost:5018"
```
Swagger UI: **http://localhost:5018/swagger** (JWT `Authorize` button configured — paste `Bearer <token>` from `/auth/login`). Swagger spec: `http://localhost:5018/swagger/v1/swagger.json`.

## 8. Starting Angular

```
cd SGInsuranceWEB/sg-insurance
npm install
ng serve
```
Runs at **http://localhost:4200**. `environment.development.ts` points at `http://localhost:5018/api/v1` (corrected during verification — see §11).

## 9. Business flow

`Quote → Proposal → KYC → Risk Verification → Payment → Policy → Document → Notification → Audit`, generalized across all 8 LOBs:

- **Generalized** (same code path for every LOB): the state machine (`DRAFT → SUBMITTED → KYC_PENDING → VERIFICATION_PENDING → PAYMENT_PENDING → PAYMENT_FAILED → COMPLETED`), KYC, payment (3-attempt limit, policy auto-issued on success), policy numbering (`SG-{LOB}-{YYYY}-{seq}`), notifications, audit logging, and the `IRatingStrategy` factory dispatch (one class per LOB, resolved from `product_master.rating_strategy_key`).
- **LOB-specific**: the `productData` JSON shape and its FluentValidation required-keys rules (`LobProductDataRules`), the 8 rating strategy implementations' actual premium math, the risk-verification response shape per `verificationType`, and — in Angular — the Step 2/Step 9 dynamic form components keyed by `lobCode`.

Verified live end-to-end for **three different LOBs** (not just motor): `PRIVATE_CAR` (MOTOR) → policy `SG-MOTOR-2026-000010`; `INDIVIDUAL_HEALTH` (HEALTH) → `SG-HEALTH-2026-000008`; `HOME_CONTENTS` (HOME) → `SG-HOME-2026-000009`. Admin dashboard confirmed live totals after these runs: 12 customers, 25 products, 21+ quotes, 20+ proposals/payments/policies, ~₹189k total premium.

## 10. Test results

- `dotnet test`: **30/30 passing** (25 unit — one per rating strategy plus PolicyNumberGenerator/PaymentService/ProposalService/QuoteService; 5 integration — full flow against the live seeded `SGInsurance` DB for 3 LOBs).
- `ng test` (Vitest, Angular's default test runner): **1/1 passing**.

## 11. Build results

- `dotnet build`: 0 errors (2–3 harmless NuGet version-unification warnings on `Microsoft.EntityFrameworkCore.Relational` — a transitive 9.0.1 vs. pinned 9.0.4, doesn't affect behavior).
- `ng build`: succeeds, 0 errors/warnings. Initial bundle ~554 kB raw / ~141 kB gzipped; largest lazy chunk is the buy-flow stepper (~39 kB gzipped).
- **Bugs found and fixed during integrated verification** (both build agents worked in isolation and hadn't been run against each other before this pass):
  1. **JWT claim mapping** — the JWT bearer handler remaps `sub` → `ClaimTypes.NameIdentifier` by default; `CurrentUserId` only read the raw `sub` claim, so it silently resolved to `Guid.Empty` for every authenticated request. Fixed with `options.MapInboundClaims = false`. This was a serious, load-bearing bug (broke `/auth/me` and anything scoped to "current user").
  2. **Quote creation required a `customerId` the client never sends** (per spec, the quote request body only carries `productCode`/`lobCode`/`productData`/`addOns`) — `QuotesController` now resolves it server-side from the JWT via `ICustomerRepository`.
  3. **HOME LOB validation over-required `sumInsuredStructure`** even for `HOME_CONTENTS`-only quotes, which only need `sumInsuredContents` — loosened to require at least one of the two, matching what the rating service already does.
  4. **Angular `environment.ts`/`environment.development.ts` pointed at port 5000**, but the API's `launchSettings.json` runs it on port **5018** — corrected both files.
  5. **`.gitignore` anchoring bug in SGInsuranceAPI** — `generated-documents/*` was rooted at the repo root, but the folder actually lives under `src/SGInsurance.Api/`, so runtime-generated emails/policy HTML were never actually ignored. Fixed with `**/generated-documents/*` and untracked the accidentally-committed test artifacts.

## 12. Git status

| Repo | Last commit | Remote |
|---|---|---|
| SGInsuranceDB | `feat(db): add multi-LOB product and policy schema with seed data` | none configured |
| SGInsuranceAPI | `chore(api): fix generated-documents gitignore anchoring, untrack test artifacts` | none configured |
| SGInsuranceWEB | `fix(web): point environment apiUrl at the API's actual port 5018` | none configured |

No repo has a remote configured yet — add one (`git remote add origin <url>`) whenever you're ready to push.

## 13. Learning guide

`SGInsuranceAPI/docs/LEARNING-GUIDE.md`

## 14. Known limitations

- **Deviation from spec**: built on **.NET 9** (net9.0), not .NET 8 — no .NET 8 SDK was installed in this environment and the user approved this substitution up front.
- KYC, risk verification, and payment gateway are all **dummy/deterministic simulations** — no real PAN/Aadhaar/RTO/PG integration.
- No real email delivery — `NotificationService` writes rendered HTML to `generated-documents/emails/` and a DB row instead.
- No real PDF generation or digital signature — the "policy document" is a rendered HTML file.
- Rating rules are simplified, config-driven approximations, not actuarially accurate.
- EF Core migration (`InitialCreate`) exists for tooling/learning purposes but was not applied to the live DB — the schema was built via the SQL scripts, and the migration's schema is reconciled against that live DB by the integration tests rather than by `dotnet ef database update`. This intentional duplication is documented in `docs/DATABASE.md`.
- Angular's `ProposalService` tracks a customer's own proposal IDs in `localStorage` (there's no "list my proposals" customer-scoped endpoint in the spec) rather than misusing the admin-only `/admin/proposals` endpoint.
- Single-machine only — everything runs on `localhost`, no deployment topology.
