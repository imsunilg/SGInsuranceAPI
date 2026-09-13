# Business flow

SGInsurance models one generalized insurance-buying journey that works
identically across all 8 lines of business (LOBs) and 25 products - the only
thing that changes per LOB is the `productData` JSON shape and the rating
strategy's math, not the flow itself.

```
Quote -> Proposal -> KYC -> Risk Verification -> Payment -> Policy -> Document -> Notification -> Audit
```

## 1. Quote

`POST /api/v1/quotes` - the customer picks a product, fills in LOB-specific
`productData` (e.g. vehicle details for MOTOR, family members for HEALTH),
and optionally selects add-ons. `PremiumCalculationService` resolves the
right `IRatingStrategy` and returns a full premium breakdown
(`sumInsured`, `basePremium`, `addonPremium`, `discount`, `gstAmount`,
`totalPremium`). A `QUOTE_CREATED` notification fires.

## 2. Proposal (state machine)

`POST /api/v1/proposals` turns a quote into a proposal in `DRAFT`. The
proposal then moves through an **explicit** state machine enforced in
`ProposalService.EnsureTransitionAllowed`:

```
DRAFT
  --submit--> SUBMITTED --(implicit)--> KYC_PENDING
KYC_PENDING
  --KYC verified--> VERIFICATION_PENDING
VERIFICATION_PENDING
  --risk verification passed--> PAYMENT_PENDING
PAYMENT_PENDING
  --payment success--> COMPLETED
  --3rd failed/timeout payment attempt--> PAYMENT_FAILED
PAYMENT_FAILED
  --new payment attempt--> PAYMENT_PENDING
```

Any transition not listed above is rejected with a clear
`InvalidOperationException` (surfaced as HTTP 400 by the global exception
middleware) - e.g. you cannot submit an already-`COMPLETED` proposal, or
jump straight from `DRAFT` to `PAYMENT_PENDING`.

## 3. KYC (dummy, deterministic)

`POST /api/v1/proposals/{id}/kyc/verify` takes a PAN + name + dob + address.
The rule is deterministic and intentionally simple: a PAN matching
`^[A-Z]{5}[0-9]{4}[A-Z]{1}$` is `VERIFIED` (and advances the proposal to
`VERIFICATION_PENDING`), anything else is `FAILED` (the proposal stays in
`KYC_PENDING` so the customer can retry). A `KYC_VERIFIED` notification
fires on success.

## 4. Risk verification (dummy, generalized per LOB)

`POST /api/v1/proposals/{id}/verification/verify` takes a
`verificationType` (`VEHICLE` / `PROPERTY` / `HEALTH_DECLARATION` /
`BUSINESS` / `CROP` / `LIVESTOCK` / `TRIP`) plus free-form
`verificationData`. The dummy rule: `PASSED` unless the client explicitly
sends `{"forceFail": true}` in `verificationData` (useful for testing the
failure path without a real underwriting engine). On `PASSED` the proposal
moves to `PAYMENT_PENDING`.

## 5. Payment (dummy gateway, max 3 attempts)

`POST /api/v1/payments` accepts a `simulateResult` field
(`SUCCESS`/`FAILED`/`TIMEOUT`) so a frontend can offer explicit "Simulate
Success/Failure/Timeout" buttons. Every attempt is recorded in
`payment_attempts`. On `SUCCESS` at *any* attempt the proposal becomes
`COMPLETED`, a `PAYMENT_SUCCESS` notification fires, and the policy is
issued immediately. On the **3rd** `FAILED`/`TIMEOUT` attempt the proposal
becomes `PAYMENT_FAILED` and a `PAYMENT_FAILED` notification fires;
`POST /api/v1/payments/{id}/retry` lets the customer try again (up to the
3-attempt cap) while the proposal is still `PAYMENT_PENDING`.

## 6. Policy issuance

`PolicyService.IssueAsync` runs automatically the moment a payment
succeeds. `PolicyNumberGenerator` produces a number in the format
`SG-{LOB}-{YYYY}-{sequence}` (sequence per LOB per year, e.g.
`SG-MOTOR-2026-000001`), a `policies` row is created with a 1-year risk
period from today, and a `POLICY_ISSUED` notification fires.

## 7. Document

`PolicyService.GenerateDocumentAsync` writes a plain HTML policy summary
under `generated-documents/` and records it in `policy_documents`.
`GET /api/v1/policies/{id}/document` returns that HTML directly.

## 8. Notification

Every step above that fires a notification goes through the single
`INotificationService.SendAsync` (see LEARNING-GUIDE.md) which looks up the
matching `notification_templates` row, substitutes `{{placeholders}}`,
writes a `notifications` row, and writes an `audit_logs` row - so every
notification is also an audit trail entry.

## 9. Audit

Every notification send (and nothing else, in this learning-scope
implementation) writes an `audit_logs` row via `NotificationService`. The
`GET /api/v1/admin/audit-logs` endpoint exposes the most recent 200 entries
for the admin back office.

## How this generalizes across LOBs

Nothing in `ProposalService`, `PaymentService`, or `PolicyService` branches
on LOB. The only LOB-aware pieces are:

- `IRatingStrategyFactory` picking the right `IRatingStrategy` (Phase 7).
- `QuoteService.ResolveSumInsured` picking the right `productData` field(s)
  to treat as the sum insured for a given LOB.
- `LobProductDataRules` (validation) checking the right required keys per
  LOB.

Everything else - the state machine, KYC, payment attempts, policy
numbering, document generation, notifications, audit - is one code path
shared by all 25 products, which is exactly what the integration tests in
`tests/SGInsurance.IntegrationTests/FullLifecycleFlowTests.cs` prove by
running the whole flow for a MOTOR, a HEALTH, and a HOME product.
