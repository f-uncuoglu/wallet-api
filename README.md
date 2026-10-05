# Wallet API

A wallet and money transfer REST API in C# with ASP.NET Core (.NET 10), Entity Framework Core and PostgreSQL.

Users register, open accounts, deposit, withdraw and transfer money to other accounts. Most of the work went into the failure cases: two requests hitting the same account at once, a client retrying a request that already went through, and a transfer failing half-way.

## Features

- Accounts, deposits, withdrawals and transfers, with an append-only transaction ledger
- Transfers are atomic: both balance updates and the ledger rows commit in one database transaction
- Optimistic concurrency on balances (PostgreSQL `xmin` row version) with automatic retry, so concurrent requests cannot overdraw an account or lose an update
- Idempotent transfers: a retried request with the same `Idempotency-Key` moves the money only once
- JWT authentication and roles: users only see their own accounts, an admin role can list all of them
- FluentValidation, and one error format (RFC 7807 `ProblemDetails`) for every failure
- Paged and filtered transaction history backed by a composite index
- Unit and integration tests (xUnit, Moq, `WebApplicationFactory`) that run against a real PostgreSQL database
- Docker Compose setup and a GitHub Actions workflow that builds and tests every push

## Tech stack

| Area | Choice |
|---|---|
| Framework | ASP.NET Core Web API (controllers), .NET 10 |
| Data | Entity Framework Core + Npgsql, PostgreSQL 17 |
| Auth | JWT bearer tokens, ASP.NET Core `PasswordHasher` (PBKDF2) |
| Validation | FluentValidation |
| API docs | OpenAPI document + Scalar UI |
| Tests | xUnit, Moq, Microsoft.AspNetCore.Mvc.Testing |
| Delivery | Docker, Docker Compose, GitHub Actions |

## Getting started

### Option 1: everything in Docker

```bash
docker compose up --build
```

The API listens on http://localhost:8080 and applies its database migrations on startup.
Open http://localhost:8080/scalar for the interactive API reference.

### Option 2: API on the host, database in Docker

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
docker compose up -d db
dotnet run --project src/Wallet.Api
```

The API listens on http://localhost:5080 (API reference: http://localhost:5080/scalar).

In Development an admin user is seeded: `admin@wallet.local` / `Admin123!`.
[`requests.http`](requests.http) contains a ready-to-run walkthrough of every endpoint.

### Tests

```bash
docker compose up -d db
dotnet test
```

The integration tests create their own database (`wallet_test`) on the same PostgreSQL server.
Set `WALLET_TEST_DB` to point them at a different server.

## API

All endpoints except `/api/auth/*`, `/api/ping` and `/health` require `Authorization: Bearer <token>`.

| Method | Route | Description |
|---|---|---|
| POST | `/api/auth/register` | Create a user |
| POST | `/api/auth/login` | Exchange email and password for a JWT |
| POST | `/api/accounts` | Open an account (`TRY`, `USD` or `EUR`), balance starts at 0 |
| GET | `/api/accounts` | List my accounts |
| GET | `/api/accounts/{id}` | Get one of my accounts |
| POST | `/api/accounts/{id}/deposits` | Deposit money |
| POST | `/api/accounts/{id}/withdrawals` | Withdraw money |
| GET | `/api/accounts/{id}/transactions` | Transaction history: `page`, `pageSize`, `type`, `from`, `to` |
| POST | `/api/transfers` | Transfer between accounts (requires `Idempotency-Key` header) |
| GET | `/api/transfers/{id}` | Get a transfer I sent or received |
| GET | `/api/admin/accounts` | All accounts of all users (admin role) |
| GET | `/health` | Liveness probe |

### Example

```bash
# Log in and keep the token
TOKEN=$(curl -s http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"ada@example.com","password":"Passw0rd!"}' | jq -r .accessToken)

# Transfer 250.00 from one account to another
curl -i http://localhost:5080/api/transfers \
  -H "Authorization: Bearer $TOKEN" \
  -H "Idempotency-Key: 7d1c6f0e-2b0a-4c3e-9a55-1f2e3d4c5b6a" \
  -H "Content-Type: application/json" \
  -d '{"fromAccountId":"<id>","toAccountId":"<id>","amount":250.00,"description":"Monthly savings"}'
```

The first call returns `201 Created`. Sending exactly the same request again returns `200 OK` with the same transfer and an `Idempotent-Replayed: true` header, and no money moves.

### Errors

Every error has the same shape (`application/problem+json`):

```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "Insufficient funds",
  "status": 422,
  "detail": "Account '0199…' has insufficient funds: balance is 30.00, requested 30.01."
}
```

| Status | Meaning |
|---|---|
| 400 | Malformed or invalid input (with a per-field `errors` object) |
| 401 | Missing, expired or invalid token; wrong credentials |
| 403 | Authenticated but lacking the required role |
| 404 | The resource does not exist **or belongs to somebody else** |
| 409 | Conflict: duplicate email, reused idempotency key, too much contention on an account |
| 422 | A business rule was violated: insufficient funds, currency mismatch |

## Project structure

```
src/Wallet.Api
├── Controllers/      HTTP only: bind the request, call a service, choose the status code
├── Services/         Business logic and use cases (one interface per service)
├── Entities/         Domain model; Account owns the "balance never goes negative" rule
├── Data/             DbContext, entity configurations, migrations, concurrency retry
├── Dtos/             Request and response contracts (entities are never exposed)
├── Validators/       FluentValidation rules for the request DTOs
├── Exceptions/       Domain exceptions, each mapped to an HTTP status
├── Infrastructure/   Exception handler, validation filter, JWT options
└── Program.cs        Composition root: DI registrations and the middleware pipeline

tests/Wallet.Tests
├── Unit/             Domain rules, validators, token service, controller (mocked service)
└── Integration/      Full HTTP pipeline against a real PostgreSQL database
```

Dependencies point inwards: controllers depend on service interfaces, services on the `DbContext` and the entities. Services use the `DbContext` directly instead of wrapping it in repositories, because the `DbContext` already is a unit of work and `DbSet<T>` already is a repository; another layer would add code without adding behaviour here.

## Design decisions

### Money is `decimal` / `numeric(18,2)`

`double` is binary floating point: `0.1 + 0.2 != 0.3`. `decimal` in C# and `numeric` in PostgreSQL are exact for base-10 fractions. Amounts with more than two decimal places are rejected at the API boundary.

### A transfer is atomic

A transfer changes two balances and inserts three rows (the transfer plus one ledger entry per side). All five statements are sent by a single `SaveChangesAsync`, which EF Core wraps in one database transaction: either everything is committed or nothing is. There is no state in which the money has left one account but not arrived in the other.

### Concurrency: optimistic, using `xmin`

The classic bug is a *lost update*: two requests read a balance of 100, both subtract 80, both write 20, and 160 has been paid out of 100.

`Account.Version` is mapped to PostgreSQL's `xmin` system column, which changes every time a row is updated. EF Core adds it to every update:

```sql
UPDATE "Accounts" SET "Balance" = @new WHERE "Id" = @id AND xmin = @versionThatWasRead;
```

If another transaction modified the row in the meantime, zero rows match, EF Core throws `DbUpdateConcurrencyException`, the transaction is rolled back, and `ConcurrencyRetry` re-runs the whole operation with fresh data (up to 5 attempts with a small random back-off, then `409 Conflict`).

*Why not `SELECT … FOR UPDATE`?* Pessimistic locking would also be correct and behaves better when one account is extremely contended. Optimistic concurrency holds no locks between the read and the write and suits a workload where most requests touch different accounts.

The table also has a `CHECK ("Balance" >= 0)` constraint, so a bug in the application still cannot store a negative balance.

### Idempotency

Networks fail *after* the server has done the work. A client that times out cannot know whether its transfer happened, so it must be able to retry safely.

Every `POST /api/transfers` carries a client-generated `Idempotency-Key`. The key is stored on the transfer and there is a **unique index on (user, key)**:

- the key was seen before with the same payload → the original transfer is returned (`200`), nothing is executed
- the key was seen before with a *different* payload → `409 Conflict` (a client bug, not a retry)
- two requests with the same key arrive at the same time → both may start executing, but only one can commit; the other is rejected by the row version check or by the unique index, rolled back, and answered with the winner's result

The guarantee comes from the database constraint, not from an application-level "check then insert", which would be a race condition.

### Authorization and information leaks

- Accounts are always queried with `WHERE Id = @id AND UserId = @currentUser`. Somebody else's account returns `404`, not `403`, so the API does not confirm which ids exist.
- Login returns the same `401` for an unknown email and for a wrong password.
- Request DTOs contain only the fields a client may set. `CreateAccountRequest` has no `Balance`, so it cannot be over-posted.
- Passwords are hashed with ASP.NET Core's `PasswordHasher` (PBKDF2, per-user salt).
- Unhandled exceptions are logged with their stack trace but returned as a generic `500`.

### Validation and error handling

Input validation (shape of the request) lives in FluentValidation validators and runs in an action filter before the controller. Business rules (state of the system) live in the services and entities and are reported by throwing a `DomainException`. A single `IExceptionHandler` converts those into `ProblemDetails`, so controllers contain no `try/catch` and no error formatting.

## Limitations and possible next steps

- Deposits and withdrawals are not idempotent yet; the same mechanism as for transfers would apply
- No refresh tokens; the access token simply expires after 60 minutes
- Transfers require both accounts to use the same currency (no FX)
- Offset paging is fine for this size; keyset paging would scale better for very long histories
- No rate limiting on the auth endpoints
