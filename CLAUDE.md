# FlowBox

Cargo/logistics backend (.NET 9, Minimal API, PostgreSQL, JWT). A learning project. To get oriented, **first** read
`docs/HANDOFF.md` (current state, what was done last, what comes next, known gaps), then load
`.claude/skills/flowbox-orientation/SKILL.md`, which routes to the topic skills (architecture, domain, auth, testing,
operations). Load only the topic skills the task needs.

## Working style

- Talk to the user in **Turkish**. Code and identifiers are in English; validation/error messages returned to API
  clients are in Turkish.
- Work in small, verifiable steps. Do not produce big phase plans. Suggest a technology (Kafka, Redis, etc.) only once
  the problem it solves has actually shown up.
- If a decision is genuinely the user's to make (e.g. 403 vs 404), ask with a short recommendation; decide the rest yourself.
- **Do not add comments to new code.**
- The user makes the commits. Never run `git commit` / `git push` unless asked. Every feature goes to `main` through its
  own branch + PR; never change `main` directly.
- Never write passwords, secrets or tokens in replies. Dev values live in `appsettings.Development.json`.
- Documentation files (`CLAUDE.md`, skills, `docs/`) are written in English.
- Skills: one topic per skill (`.claude/skills/flowbox-<topic>/SKILL.md`), under ~100 lines, with a trigger-word
  `description`; add each new skill to the routing table in `flowbox-orientation`. Durable facts go in skills; anything
  that changes between sessions (state, next steps, known gaps, test counts) goes in `docs/HANDOFF.md`.

## Code rules

- Layer flow: `Endpoint` (HTTP only) → `Service` (business rules, `IService`, concrete class) → `IRepository` → EF Core.
  Naming follows the bubilet-panel repo; write new files in the same pattern as the existing examples.
- Register every new repository by hand in `AddRepositories()` (services are registered automatically via reflection).
- Write entity types fully qualified inside feature namespaces (`Data.Ef.Models.Shipment`), otherwise you get `CS0118`.
- Give every new endpoint a deliberate authorization rule (`RequireAuthorization`). Auth is opt-in: if you forget, the
  endpoint is anonymous.
- Migrations: generate with `dotnet ef migrations add` and read the result. Never delete a migration file by hand; use
  `dotnet ef migrations remove`. Adding a required FK/column breaks local data, so tell the user that
  `docker compose down -v` is needed.

## Tests and verification

- New feature = **unit + integration tests**. Run both before finishing and report the result honestly.
- Integration tests need Docker. `FlowBox.sln` is at the repo root, `docker-compose.yml` is inside `src/`.

```bash
dotnet test src/FlowBox.UnitTests/FlowBox.UnitTests.csproj
dotnet test src/FlowBox.IntegrationTests/FlowBox.IntegrationTests.csproj
```

## Workflows

**New feature**
1. Start once the user says the branch is open. If you are on `main`, warn first.
2. Read the skill and the relevant existing files, then write in the same pattern.
3. Order: entity/enum → repository (+ DI registration) → service → endpoint + validator → migration → tests.
4. Run `dotnet build` and both test suites; do not say "done" until everything is green.
5. Give a summary: what changed, what you deliberately left open, the steps the user must take (including a DB reset if
   needed) and the commit commands.

**Debugging**
1. Find the real cause first: logs, stack trace, `docker compose logs api`. Do not change code on a guess.
2. Separate environment problems (Docker down, wrong directory, stale image) from code problems. For environment
   problems, tell the user what to do.
3. After fixing, verify the same way and state clearly what was broken.

**Schema change**
1. Change the entity and run `dotnet ef migrations add <Name>`.
2. Read the generated migration. If it contains something like `defaultValue: Guid.Empty`, or existing rows could
   violate a FK, tell the user.
3. Check whether the local DB has data; if needed, ask for `docker compose down -v` (look at what is there before wiping).
4. Integration tests apply migrations from scratch, so they are a second verification.

**Load test**
- `k6 run load-tests/tracking-lookup.js` (the API must be up on `localhost:8080`). Summarize with p95 and error rate;
  if the system did not break, do not propose new technology.

**Delivery**
1. Update `docs/HANDOFF.md` (where we are, what was just done, what is next, open decisions) as part of the change.
2. Give the user these commands (do not run them yourself): `git add -A`, `git commit -m "add ..."` with a short
   English message, `git push -u origin <branch>`.
3. The user opens the PR and merges once CI is green.
4. After the merge: `git checkout main`, `git pull`, new branch.
