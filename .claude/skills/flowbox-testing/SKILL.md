---
name: flowbox-testing
description: >-
  FlowBox tests, CI and load testing: unit vs integration layout, fake repositories, Testcontainers,
  TestAuthHelper, GitHub Actions workflow and the k6 tracking-lookup script. Use when writing or running tests,
  debugging a failing test or CI run, or running a load test.
---

# FlowBox — testing

- `FlowBox.UnitTests` (**no Docker**, seconds): `Domain/` (status rules), `Service/<Feature>/` (business rules with
  fake repositories), `Validators/`, `Fakes/` (`Fake*Repository`, hand-written, in-memory lists).
- `FlowBox.IntegrationTests` (**Docker required**): `WebApplicationFactory` + Testcontainers with a real Postgres,
  one container per test class, migrations applied from scratch (a second check on migrations).
- `TestAuthHelper.AuthenticateAsAsync(role)` gets a token through the real flow (Customer: register; Courier/Admin:
  the seed Admin's `/api/admin/users`). `CreateCourierAsync` returns the new `courierId`; `AssignShipmentAsAdminAsync`
  assigns a shipment (a courier can update status only after it is assigned to them). The factory overrides
  `AdminSeed` with test values.
- New feature = both unit and integration tests. Do not report done until both suites are green. Current counts live in
  `docs/HANDOFF.md`.

```bash
dotnet test src/FlowBox.UnitTests/FlowBox.UnitTests.csproj
dotnet test src/FlowBox.IntegrationTests/FlowBox.IntegrationTests.csproj
```

Run from the repo root (`FlowBox.sln` is there). Integration tests need Docker running.

## CI

`.github/workflows/ci.yml`: on push and PR to `main`, restore -> build -> unit -> integration (runner's Docker).
Every feature has its own branch + PR; merge once CI is green.

## Load test (k6)

`load-tests/tracking-lookup.js` registers its own Customer and loads `GET /api/shipments/{tn}` at 20 -> 100 -> 300
VUs. Baseline: p95 about 8–14 ms, 0% errors, about 125 req/s. The API must be up on `localhost:8080`:

```bash
k6 run load-tests/tracking-lookup.js
```

Summarize with p95 and error rate. If the system did not break, do not propose new technology.
