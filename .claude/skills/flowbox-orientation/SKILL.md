---
name: flowbox-orientation
description: >-
  Entry point for the FlowBox logistics API (.NET 9, PostgreSQL, JWT). Read first when starting any FlowBox work or
  returning after a break; it says what the project is and which topic skill to load next.
---

# FlowBox — orientation

FlowBox is a **learning project** that simulates a real cargo/logistics backend. The goal is not CRUD; it is to add
topics such as event-driven architecture, caching, security and scaling **when a real problem shows up**. Working
style: "what are we doing today" in small, verifiable steps. No big phase plans; a technology is added only when the
problem it solves has been observed (e.g. the load test showed no need for Redis, so there is no Redis).

Stack: .NET 9, ASP.NET Core Minimal API, EF Core 9 + Npgsql (PostgreSQL 16), FluentValidation, JWT Bearer, OpenAPI +
Scalar (`/scalar/v1`), Docker + docker-compose, xUnit + FluentAssertions, Testcontainers, GitHub Actions, k6.

## Which skill to load

| If the task is about... | Load |
|---|---|
| Folder map, layers, DI, adding an endpoint/service/repository, code pitfalls | `flowbox-architecture` |
| Order, Shipment, Courier, User, status pipeline | `flowbox-domain` |
| Endpoint roles, ownership (404 rule), JWT, AdminSeed | `flowbox-auth` |
| Unit/integration tests, `TestAuthHelper`, CI, k6 load test | `flowbox-testing` |
| Docker, docker-compose, migrations, resetting the DB, paths | `flowbox-operations` |

Changing behavior usually needs two skills (e.g. a new protected endpoint: `flowbox-architecture` + `flowbox-auth`).

## Not in skills

- Current state, what is next, open decisions, known gaps: `docs/HANDOFF.md` (volatile).
- Rules for Claude's behavior (language, commits, workflows): `CLAUDE.md`.
- User-facing run instructions: `README.md`.

## Rules for adding or changing skills

- One topic per skill, under ~100 lines, in English.
- `description` states the topic and trigger words; the body is loaded only when it matches.
- Add every new skill to the table above.
- Put durable facts in skills; put anything that changes between sessions in `docs/HANDOFF.md`.
