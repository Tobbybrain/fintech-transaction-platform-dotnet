# Fintech Transaction Platform — ASP.NET Core / PostgreSQL

A focused backend engineering portfolio project demonstrating REST APIs, C#/.NET, relational transactions, concurrency protection, idempotency, authentication/authorization, webhook delivery, Docker, and failure-handling patterns relevant to fintech platforms.

## What this demonstrates

- **C# / ASP.NET Core 8** API design with controllers and dependency injection.
- **PostgreSQL + EF Core** relational persistence.
- **Financial transaction safety** using a serializable database transaction and deterministic `SELECT ... FOR UPDATE` row locking.
- **Idempotency** through a required `Idempotency-Key` and a unique database constraint so retries cannot create duplicate transfers.
- **Double-entry audit trail**: every transfer writes a debit entry and a matching credit entry in the same transaction.
- **Optimistic concurrency signal** through a wallet version concurrency token, plus pessimistic row locking for money movement.
- **JWT authentication and RBAC** with User/Admin roles.
- **Webhook outbox pattern** so the transfer commits independently of external webhook availability.
- **Signed webhooks** using HMAC-SHA256, retry/backoff and dead-letter state.
- **Basic SSRF protections** for webhook destinations by requiring HTTPS and rejecting loopback/private IP resolutions.
- **Docker / Docker Compose** for API + PostgreSQL.
- **Automated tests and CI** on GitHub Actions.

See [ARCHITECTURE.md](ARCHITECTURE.md) for design decisions and documented hardening boundaries.

## Architecture

```text
HTTP API
  |-- AuthController -------- JWT / RBAC
  |-- WalletsController ----- balances + ledger history
  |-- TransfersController --- idempotent money movement
                                |
                                v
                         TransferService
                         | serializable tx
                         | row locks
                         | balance updates
                         | debit + credit entries
                         | webhook outbox record
                                |
                                v
                           PostgreSQL
                                |
                                v
                      WebhookDeliveryService
                      HMAC signature / retries
```

## Run with Docker

Copy the environment template and replace every placeholder with your own local values:

```bash
cp .env.example .env
docker compose up --build
```

API: `http://localhost:8080`  
Swagger: `http://localhost:8080/swagger`  
Health: `http://localhost:8080/health`

Demo seeding is opt-in and enabled by Docker Compose only when `DEMO_PASSWORD` is supplied through the local `.env` file. The seeded local accounts are:

- `user@demo.local`
- `admin@demo.local`

Both use the password you set in `DEMO_PASSWORD`. No reusable demo password is committed to the repository.

## Demo flow

### 1. Get a JWT

```bash
curl -X POST http://localhost:8080/api/auth/token \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"user@demo.local\",\"password\":\"$DEMO_PASSWORD\"}"
```

### 2. List wallets

```bash
curl http://localhost:8080/api/wallets \
  -H "Authorization: Bearer $TOKEN"
```

### 3. Make an idempotent transfer

```bash
curl -X POST http://localhost:8080/api/transfers \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: demo-transfer-001' \
  -d '{
    "fromWalletId":"SOURCE_WALLET_GUID",
    "toWalletId":"DESTINATION_WALLET_GUID",
    "amount":1500.00,
    "webhookUrl":null
  }'
```

Repeat the same request with the same key: the API returns the original transfer instead of debiting twice.

## Transaction integrity

The transfer service performs the following inside one **serializable PostgreSQL transaction**:

1. Re-checks the idempotency key.
2. Locks both wallet rows in deterministic GUID order using `FOR UPDATE`.
3. Verifies both wallets exist and use the same currency.
4. Rejects insufficient funds.
5. Debits the source and credits the destination.
6. Writes matching debit and credit ledger entries.
7. Creates a webhook outbox record when a callback is requested.
8. Commits everything atomically.

The unique idempotency-key constraint is the final protection against simultaneous retries racing each other.

## Webhook reliability

Webhooks are not called inside the transfer transaction. Instead, the transaction writes an outbox record. A background worker validates an HTTPS destination, rejects loopback/private IP resolutions, signs the payload with HMAC-SHA256, sends with a short timeout, retries failures with exponential backoff, and moves permanently failing deliveries to dead-letter state.

The URL policy is intentionally documented as a basic SSRF defense, not a complete network-isolation guarantee. See [ARCHITECTURE.md](ARCHITECTURE.md).

## Verification

GitHub Actions runs:

- dependency restore
- Release build
- xUnit tests
- transfer authorization/import checks
- public-release hygiene checks

See [VERIFICATION.md](VERIFICATION.md) for the exact workflow evidence and current scope.

## Tests

```bash
dotnet test tests/FintechPlatform.Tests/FintechPlatform.Tests.csproj
```

## Status

Portfolio implementation. It is deliberately compact enough to explain end-to-end while demonstrating transaction, concurrency, security, integration, and reliability patterns relevant to a fintech backend.
