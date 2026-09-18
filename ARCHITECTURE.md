# Architecture and Design Decisions

## Purpose

This project is a compact ASP.NET Core fintech backend that demonstrates safe money-movement patterns, authenticated REST APIs, relational persistence, webhook reliability, and containerized local development.

## Transaction boundary

A transfer is executed inside one PostgreSQL serializable transaction. The service re-checks the idempotency key, locks both wallet rows in deterministic GUID order with `FOR UPDATE`, validates balances and currency, updates both balances, writes matching debit and credit ledger entries, optionally creates a webhook outbox record, and then commits atomically.

## Idempotency

Every transfer request requires an `Idempotency-Key`. The application checks for an existing transfer before and inside the transaction, while a unique database index provides the final race-condition guard.

## Concurrency

Money movement uses pessimistic row locking so concurrent transfers cannot mutate the same wallet balances simultaneously. Wallets also carry a version concurrency token as an additional signal for conflicting updates.

## Authorization

JWT bearer authentication is required for wallet and transfer APIs. Non-admin users can operate only on wallets they own and can retrieve only transfers whose source wallet they own.

## Webhooks

External callbacks are decoupled from the transfer transaction through an outbox record. A background worker signs payloads with HMAC-SHA256, enforces HTTPS destinations, rejects loopback/private IP resolutions, applies timeouts, retries failed deliveries with exponential backoff, and moves exhausted deliveries to dead-letter state.

The destination policy is a basic SSRF defense rather than a complete network-isolation guarantee. Production hardening would pin/verify resolved destinations at connect time or route outbound callbacks through a controlled egress layer.

## Configuration

Runtime credentials and signing material are supplied through environment variables. No production or reusable secrets belong in source control. Optional demo seeding is disabled by default and only activates when explicitly enabled with an externally supplied password.

## Verification

GitHub Actions runs restore, Release build, xUnit tests, authorization/import static checks, and public-readiness hygiene checks on `main` and pull requests.
