# Build & Test Verification

This repository is verified by its own GitHub Actions workflow using Microsoft's hosted Ubuntu runner and the .NET 8 SDK.

## Verification workflow

Repository: `Tobbybrain/fintech-transaction-platform-dotnet`  
Workflow: `.NET CI`

The workflow executes:

```bash
dotnet restore tests/FintechPlatform.Tests/FintechPlatform.Tests.csproj
dotnet build tests/FintechPlatform.Tests/FintechPlatform.Tests.csproj -c Release --no-restore
dotnet test tests/FintechPlatform.Tests/FintechPlatform.Tests.csproj -c Release --no-build --logger "trx;LogFileName=test-results.trx"
bash scripts/static-test-imports.sh
bash scripts/static-test-transfer-authorization.sh
bash scripts/public-readiness-check.sh
```

The workflow also uploads the xUnit TRX output as an artifact.

## Scope

A green workflow proves that the current repository state:

- restores its dependencies,
- compiles in Release configuration,
- passes its xUnit unit tests,
- retains the object-level transfer authorization check,
- retains the xUnit import check, and
- passes the public-release hygiene rules.

PostgreSQL transaction, idempotency, deterministic row locking, JWT/RBAC, transfer ownership authorization, double-entry ledger, webhook outbox/retry and basic webhook-destination SSRF protections are implemented in code.

PostgreSQL-backed integration tests remain a documented next-hardening step. This project is a **verified hands-on ASP.NET Core portfolio implementation**, not a claim of commercial production .NET experience.
