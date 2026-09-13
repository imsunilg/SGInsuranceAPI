# API reference

Base URL when run locally: `http://localhost:{port}` (see launchSettings.json
- typically `http://localhost:5xxx`; Swagger UI is at `/swagger`, raw spec at
`/swagger/v1/swagger.json`). All endpoints are versioned under `/api/v1/`.

## Response envelope

Every endpoint returns this shape:

```json
// success
{ "success": true, "data": { }, "message": null, "errors": [] }
// error
{ "success": false, "data": null, "message": "...", "errors": ["..."] }
```

## Auth

| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `/api/v1/auth/register` | none | creates a `CUSTOMER` user + linked customer record, sends `WELCOME_EMAIL` |
| POST | `/api/v1/auth/login` | none | returns a JWT |
| GET | `/api/v1/auth/me` | Bearer | current user + roles |

```json
// POST /api/v1/auth/register
{ "email": "jane@example.com", "password": "Passw0rd1", "firstName": "Jane", "lastName": "Doe", "mobile": "9876543210" }

// POST /api/v1/auth/login -> data
{ "token": "eyJ...", "expiresAt": "2026-09-14T10:00:00Z", "user": { "userId": "...", "email": "jane@example.com", "roles": ["CUSTOMER"] } }
```

Demo accounts (already seeded): `customer@sginsurance.com` / `Customer@123`
(CUSTOMER), `admin@sginsurance.com` / `Admin@123` (ADMIN).

## Products

| Method | Path | Auth |
|---|---|---|
| GET | `/api/v1/lobs` | none |
| GET | `/api/v1/products?lobCode=MOTOR` | none |
| GET | `/api/v1/products/{productCode}` | none |
| POST/PUT | `/api/v1/products` / `/api/v1/products/{productCode}` | ADMIN |
| DELETE | `/api/v1/products/{productCode}` | ADMIN (soft delete: `is_active=false`) |

## Quotes

| Method | Path | Auth |
|---|---|---|
| POST | `/api/v1/quotes` | Bearer |
| GET | `/api/v1/quotes` | ADMIN |
| GET | `/api/v1/quotes/{id}` | Bearer |
| GET | `/api/v1/quotes/customer/{customerId}` | Bearer |
| POST | `/api/v1/quotes/{id}/recalculate` | Bearer |
| DELETE | `/api/v1/quotes/{id}` | Bearer |

```json
// POST /api/v1/quotes
{
  "customerId": "c1111111-0000-0000-0000-000000000001",
  "productCode": "PRIVATE_CAR",
  "lobCode": "MOTOR",
  "productData": { "regNumber": "KA01AB1234", "make": "MARUTI", "model": "SWIFT", "year": 2021, "fuelType": "PETROL", "sumInsured": 500000, "ncbPercent": 1 },
  "addOns": ["ZERO_DEP"]
}
// -> data
{
  "quoteId": "...", "productCode": "PRIVATE_CAR", "productName": "...", "lobCode": "MOTOR",
  "sumInsured": 500000, "basePremium": 15150, "addonPremium": 1200, "discount": 3787.5,
  "gstAmount": 2263.35, "totalPremium": 14825.85, "status": "CREATED", "expiresAt": "..."
}
```

## Proposals / KYC / Verification

| Method | Path | Auth |
|---|---|---|
| POST | `/api/v1/proposals` | Bearer |
| GET/PUT | `/api/v1/proposals/{id}` | Bearer |
| POST | `/api/v1/proposals/{id}/submit` | Bearer |
| POST | `/api/v1/proposals/{id}/kyc/verify` | Bearer |
| POST | `/api/v1/proposals/{id}/verification/verify` | Bearer |

```json
// POST /api/v1/proposals/{id}/kyc/verify
{ "pan": "ABCDE1234F", "name": "Jane Doe", "dob": "1990-05-15", "address": "12 MG Road" }
// -> data
{ "kycId": "...", "result": "VERIFIED", "proposalStatus": "VERIFICATION_PENDING" }

// POST /api/v1/proposals/{id}/verification/verify
{ "verificationType": "VEHICLE", "verificationData": { "odometer": 12000 } }
// -> data
{ "verificationId": "...", "status": "PASSED", "proposalStatus": "PAYMENT_PENDING" }
```

## Payments

| Method | Path | Auth |
|---|---|---|
| POST | `/api/v1/payments` | Bearer |
| GET | `/api/v1/payments/{id}` | Bearer |
| POST | `/api/v1/payments/{id}/retry` | Bearer |

```json
// POST /api/v1/payments
{ "proposalId": "...", "amount": 14825.85, "mode": "UPI", "simulateResult": "SUCCESS" }
// -> data
{ "paymentId": "...", "proposalId": "...", "amount": 14825.85, "mode": "UPI", "status": "SUCCESS", "attemptCount": 1, "policyId": "...", "policyNumber": "SG-MOTOR-2026-000003" }
```

## Policies

| Method | Path | Auth |
|---|---|---|
| GET | `/api/v1/policies` | ADMIN |
| GET | `/api/v1/policies/{id}` | Bearer |
| GET | `/api/v1/policies/customer/{customerId}` | Bearer |
| GET | `/api/v1/policies/{id}/document` | Bearer (returns `text/html`) |

## Notifications

| Method | Path | Auth |
|---|---|---|
| GET | `/api/v1/notifications/customer/{customerId}` | Bearer |

## Admin

| Method | Path |
|---|---|
| GET | `/api/v1/admin/dashboard` |
| GET | `/api/v1/admin/customers`, `/products`, `/premium-rules`, `/quotes`, `/proposals`, `/payments`, `/policies`, `/notifications`, `/audit-logs` |
| PUT | `/api/v1/admin/premium-rules/{ruleId}` (body: raw JSON string, replaces `premium_rules.config`) |

```json
// GET /api/v1/admin/dashboard -> data
{ "customers": 5, "products": 25, "quotes": 12, "proposals": 9, "payments": 7, "policies": 6, "totalPremium": 123456.78 }
```

## Auth model

JWT bearer tokens, `Authorization: Bearer <token>`. Claims include `sub`
(user id), `email`, and one `role` claim per assigned role (`CUSTOMER` /
`ADMIN`). `[Authorize]` on most endpoints, `[Authorize(Roles = "ADMIN")]`
on admin-only ones. Swagger UI has an **Authorize** button wired to the same
bearer scheme.
