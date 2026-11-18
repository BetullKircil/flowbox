# FlowBox — Handoff

Volatile state only: where the project stands right now and what to do next. Durable facts (architecture, domain,
permissions, pitfalls) live in the topic skills under [`.claude/skills/`](../.claude/skills/) (start at
`flowbox-orientation`) and in the [README](../README.md); they are not repeated here.

Last updated: 2026-11-16, on branch `docs/split-skills`.

## Where we are now

- `main` includes PR #7 (handoff document, English `CLAUDE.md` and skill). Features merged so far: #1 CI, #2 k6 load
  test, #3 JWT auth (+ Postgres healthcheck), #4 restricted registration + Admin-only user creation, #5 customer
  ownership, #6 courier ownership.
- Tests: **79 unit + 33 integration**, all passing (verified 2026-11-16).
- In progress: `docs/code-reviewer-agent` adds the `code-reviewer` subagent. `feature/tracking-number-unique` is open
  but has no changes yet; merge `main` into it before starting.

## Last done

- PR #8: split `flowbox-orientation` into a router plus `flowbox-architecture`, `flowbox-domain`, `flowbox-auth`,
  `flowbox-testing`, `flowbox-operations`. Before: one skill of 161 lines / 11,117 characters loaded in full for every
  task; now about 3,800–5,500 characters per task (about 7,900 when two topics are needed). The skill listing grew by
  about 350 tokens (6 descriptions instead of 1).
- Added `.claude/agents/code-reviewer.md`: read-only (no Edit/Write), runs on `sonnet`, reviews `git diff main...HEAD`
  against `CLAUDE.md` and the topic skills. Bash is allowed for `git diff`/`log`/`status` only by instruction, not by
  enforcement. `CLAUDE.md` workflow now includes a review step before the summary.

## Next

1. **TrackingNumber uniqueness** (highest priority, branch `feature/tracking-number-unique`). It is generated in
   `OrderService` as `TR{Random.Shared.Next(100000, 999999)}` with no unique index. Needs a unique index +
   collision-resistant generation (retry or a longer value) + unit and integration tests. The migration fails if the dev
   DB already holds duplicates, so expect `docker compose down -v`. Re-run the k6 test afterwards and compare with the
   baseline in `flowbox-testing`.
2. Try the `code-reviewer` subagent on the TrackingNumber diff and compare findings and token use with `/code-review`
   in the main session. Later: `docs/decisions/` ADRs (404 vs 403, why repository interfaces are kept).
3. Token experiments, one tool at a time so effects stay attributable: baseline (no plugins) -> Superpowers -> Ponytail
   or Caveman. Measure each with `/context` or the usage tool.

## Known gaps

- Admin-placed orders are stored under the Admin account.
- No refresh token, rate limiting or account lockout; JWT lifetime is 60 minutes.
- No deployment (CI only verifies).
- DataProtection keys are not persisted in the container (warning only; nothing uses them yet).
- Vision items not started: Kafka/event-driven, Redis, Elasticsearch, observability, microservices, mobile apps,
  route optimization, AI prediction, external services (SMS, maps, payment).

## Open decisions / questions

- Should an Admin be able to place an order on behalf of a customer?
- Tracking number format once uniqueness is enforced: keep `TR` + digits or change it?

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
