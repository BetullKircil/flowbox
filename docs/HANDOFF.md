# FlowBox — Handoff

Volatile state only: where the project stands right now and what to do next. Durable facts (architecture, domain,
permissions, pitfalls, gaps) live in [`.claude/skills/flowbox-orientation/SKILL.md`](../.claude/skills/flowbox-orientation/SKILL.md)
and the [README](../README.md); they are not repeated here.

Last updated: 2026-11-16, after PR #6 (courier ownership).

## Where we are now

- `main` is at the merge of PR #6 (`135b6f8`). Everything below is merged and green.
- Merged so far: #1 CI, #2 k6 load test, #3 JWT auth (+ Postgres healthcheck), #4 restricted registration + Admin-only
  user creation, #5 customer ownership of orders/shipments, #6 courier ownership (Courier linked to User, only the
  actively assigned courier can read/update a shipment).
- Tests: **79 unit + 33 integration**, all passing (verified 2026-11-16).
- Working branch for this document: `docs/handoff`.

## Last done

- Courier ownership: `Courier.UserId` (required, unique); Admin creates a Courier account and profile in one request;
  non-assigned couriers get 404; `GET /api/couriers/me/shipments`.
- Project tooling: `CLAUDE.md` (rules), the `flowbox-orientation` skill (map/domain/pitfalls), README Docker section.
- Docs (this branch): `CLAUDE.md`, the skill and this file translated/written in English.

## Next

1. **TrackingNumber uniqueness** (highest priority, separate `feature/...` branch). Today it is generated in
   `OrderService` as `TR{Random.Shared.Next(100000, 999999)}` with no unique index, so collisions can return the wrong
   shipment and lookups degrade as the table grows. Needs a unique index + collision-resistant generation (retry or a
   longer random/sequence-based value) + tests. The migration will fail if the dev DB already holds duplicates, so
   expect `docker compose down -v`.
2. Re-run `k6 run load-tests/tracking-lookup.js` after the index lands and compare with the baseline in the skill.
3. Learning track for the repo tooling (not started): a read-only `code-reviewer` subagent
   (`.claude/agents/code-reviewer.md`), splitting the skill into topic skills with a router table, and `docs/decisions/`
   ADRs (404 vs 403, why repository interfaces are kept).

## Open decisions / questions

- Admin-placed orders are owned by the Admin account. Should an Admin be able to create an order on behalf of a customer?
- How should tracking numbers look once uniqueness is enforced (keep the `TR` + digits format or change it)?

## Watch out for

- `docker compose down -v` wipes local data; needed after adding a required FK/column or a unique index over existing data.
- Never delete migration files by hand; use `dotnet ef migrations remove`.
- Run `dotnet test src/...` from the repo root; `docker compose` commands from `src/`.
- Auth is opt-in: every new endpoint needs an explicit `RequireAuthorization`.
- The user commits and pushes; Claude only prepares the commands.

## How to continue

```bash
dotnet test src/FlowBox.UnitTests/FlowBox.UnitTests.csproj
dotnet test src/FlowBox.IntegrationTests/FlowBox.IntegrationTests.csproj
cd src && docker compose up --build -d
```

API: `http://localhost:8080`, Scalar: `/scalar/v1`. Integration tests need Docker running.
