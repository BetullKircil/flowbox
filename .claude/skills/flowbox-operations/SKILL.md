---
name: flowbox-operations
description: >-
  FlowBox local operations: docker compose, database access, EF Core migrations, resetting the local DB and
  directory/path confusion. Use when running the stack, creating or removing a migration, changing the schema, or
  debugging container/DB startup problems.
---

# FlowBox — operations

## Running

```bash
cd src && docker compose up --build -d     # API: http://localhost:8080  (Scalar: /scalar/v1)
docker compose down -v                      # resets the DB (local data is deleted)
```

- Postgres has a healthcheck; the API waits with `condition: service_healthy`.
- Inside compose the DB host is `db`. From **outside** (Rider/DataGrip) use `localhost:5432` (user/password/DB name
  are in `docker-compose.yml`).
- Debug startup problems with `docker compose logs api`; separate environment issues (Docker down, wrong directory,
  stale image) from code issues first.
- DataProtection keys are not persisted in the container; the warning is harmless today.

## Migrations

- Generate with `dotnet ef migrations add <Name>` and **read the result**. Namespace moves produce empty migrations;
  those are harmless.
- **Never delete a migration file by hand.** `add` also advances the snapshot; deleting the file leaves the snapshot
  ahead and the next migration comes out incomplete. Use `dotnet ef migrations remove` (it connects to the DB, so
  Docker must be running).
- **A required FK/column, or a unique index over existing data, breaks local data** (e.g. `Orders.CustomerId`): old
  rows make the migration fail and the API will not start. Look for `defaultValue: Guid.Empty` or FK-violation risk in
  the generated file, tell the user, check what is in the local DB, then ask for `docker compose down -v`.
- Integration tests apply migrations from scratch, so they double as verification.

## Paths

`FlowBox.sln` is **at the repo root**; `docker-compose.yml` is inside `src/`. Do not write paths starting with `src/...`
while the terminal is already inside `src/`.
