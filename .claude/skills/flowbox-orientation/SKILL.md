---
name: flowbox-orientation
description: >-
  Read BEFORE starting any work on the FlowBox project. Describes the project map, layer/naming rules, the domain
  model (Order -> Shipment, status pipeline), auth and role/ownership rules, test/migration/Docker/CI commands and
  known gaps. Use it when returning to the project after a long break, when asked "what has been done / how does it
  work", when adding an endpoint, service, repository, entity, migration, test or authorization rule, or when
  debugging a test/CI/Docker failure. Orients a fresh session on the FlowBox logistics API (.NET 9, PostgreSQL, JWT, xUnit).
---

# FlowBox — orientation

FlowBox is a **learning project** that simulates a real cargo/logistics backend. The goal is not CRUD; it is to add
topics such as event-driven architecture, caching, security and scaling **when a real problem shows up**. Working
style: "what are we doing today" in small, verifiable steps. No big phase plans; a technology is added only when the
problem it solves has been observed (e.g. the load test showed no need for Redis, so there is no Redis).

## Technologies

.NET 9, ASP.NET Core **Minimal API**, EF Core 9 + Npgsql (**PostgreSQL 16**), FluentValidation, JWT Bearer
(passwords hashed with `PasswordHasher<T>`), OpenAPI + Scalar (`/scalar/v1`), Docker + docker-compose,
xUnit + FluentAssertions, Testcontainers, GitHub Actions, k6 (load testing).

## Folder map (`src/FlowBox.Api/`)

Layer folders are flat; inside them are feature folders: `Shipment`, `Courier`, `Order`, `Auth`.
Naming is taken from the **bubilet-panel** repo at work.

| Folder | Contents |
|---|---|
| `Endpoints/<Feature>/` | `IEndpoint` classes: HTTP only (take the request, call the Service, map the result to an HTTP response) |
| `Service/<Feature>/` | Business rules. `IService` is an empty marker interface; services are injected as **concrete classes** |
| `Repositories/<Feature>/` | `IxxxRepository` + `EfXxxRepository` (not in bubilet-panel; added deliberately so unit tests run without a DB) |
| `Data/Ef/` | `FlowBoxDbContext` + `Models/` (entities) |
| `Domain/Shipment/` | Pure business rule: `ShipmentStatusTransitions` |
| `Validators/<Feature>/` | FluentValidation rules |
| `Extensions/` | `ClaimsPrincipalExtensions.ToCaller()` (builds `Caller(UserId, Role)` from the JWT) |
| `Enums/` | `ShipmentStatus`, `OrderStatus`, `UserRole` (flat, not under a module) |
| `Registry/` | `ServiceCollectionExtensions` (DI) + `EndpointRouteBuilderExtensions` (`MapEndpoints`) |
| `Migrations/` | EF migrations |

Request flow: `Endpoint (static Handle)` → `Service` → `IRepository` → `EF Core`. Handlers take their dependencies as
**handler parameters** (not constructors). Endpoints are discovered by reflection in `Program.cs` (`IEndpoint`,
parameterless constructor required); services register themselves through `AddService()` (`IService`).
**Repositories are not registered automatically**: add them by hand in `AddRepositories()`.

Service methods know nothing about HTTP; they return discriminated records (`UpdateShipmentStatusResult.Success /
ShipmentNotFound / InvalidTransition`, etc.) and the endpoint maps them to HTTP status codes with a `switch`.

## Domain

- **Order**: the customer's request, carries `CustomerId` (-> `User`). Today it turns into a Shipment synchronously
  when created (1:1). The split will pay off once steps such as payment confirmation are inserted between them.
- **Shipment**: the real tracking unit (`TrackingNumber`, `OrderId`). **ShipmentTrackingEvent**: one record per status
  change (never deleted or updated). **ShipmentAssignment**: courier assignment history.
- **User** (unique Email, PasswordHash, Role) and **Courier** (Name, Phone) are linked: `Courier.UserId` is required
  and unique. When a Courier account is created, the profile (name, phone) is supplied too; `AuthService.RegisterAsync`
  creates both in one operation. A Courier account without a profile cannot exist
  (`RegisterResult.CourierProfileRequired`).
- **Status pipeline**: `Created → PickedUp → ArrivedAtSortingCenter → Sorted → InTransit →
  ArrivedAtDistributionCenter → OutForDelivery → Delivered`. `Failed` is reachable from every non-terminal state.
  Steps cannot be skipped; nothing can change after `Delivered`/`Failed`.

## API and permissions

| Endpoint | Who |
|---|---|
| `POST /api/auth/register` | Everyone (always creates a **Customer**; a `role` in the body is ignored) |
| `POST /api/auth/login` | Everyone |
| `POST /api/admin/users` | Admin (creates a Customer/Courier/Admin account; Courier requires `name` and `phone`, the response returns `courierId`) |
| `POST /api/orders`, `GET /api/orders/mine` | Customer, Admin |
| `GET /api/shipments/{tn}`, `/tracking`, `/history` | Any authenticated user + **ownership**: Admin sees all; Customer only the shipment of their own order; Courier only a shipment **actively assigned** to them. Everyone else gets **404** (not 403, so existence is not leaked) |
| `PATCH /api/shipments/{tn}/status` | Courier (same ownership rule), Admin |
| `GET /api/couriers/me/shipments` | Courier (shipments actively assigned to them, paged) |
| `PATCH /api/shipments/{tn}/assign`, `GET /api/shipments` | Admin |

Role rules are set on the endpoint with `.RequireAuthorization(policy => policy.RequireRole(...))`; ownership rules
live in the Service (`ShipmentService.CanAccessAsync`). When a shipment is reassigned to another courier, the previous
courier loses access. Auth is **opt-in**: if you forget `RequireAuthorization` on a new endpoint, it is anonymous.

## Auth

- JWT (HS256), role claim is `ClaimTypes.Role`. `Jwt:Issuer/Audience/ExpiryMinutes` are in `appsettings.json`;
  **if `Jwt:Secret` is empty the app fails at startup**. The dev secret is in `appsettings.Development.json`; in
  production it must come from the `Jwt__Secret` environment variable.
- **First Admin**: since only an Admin can create an Admin, the `AdminSeed:Email/Password` setting creates one at
  startup (idempotent). Dev values are in `appsettings.Development.json`; in production use the `AdminSeed__Email` /
  `AdminSeed__Password` environment variables. It never changes the role of an existing user.
- A wrong password and an unknown email return the same response (401), on purpose.

## Pitfalls (rules that catch newcomers)

1. **Namespace–type collision.** Feature folder names equal entity names (`Shipment`, `Courier`, `Order`), so writing a
   bare `Order`/`Shipment`/`Courier` inside `FlowBox.Api.Service.Order` gives `CS0118 'X' is a namespace but is used
   like a type`. Write the entity **fully qualified**: `Data.Ef.Models.Shipment`. (`User` does not collide; its folder
   is `Auth`.)
2. **Never delete a migration file by hand.** `dotnet ef migrations add` also advances the snapshot; deleting the file
   leaves the snapshot ahead and the next migration comes out incomplete. Use `dotnet ef migrations remove` (it
   connects to the DB, so Docker must be running). Namespace moves produce an empty migration, which is harmless.
3. **Adding a required FK/column breaks existing local data** (e.g. `Orders.CustomerId`): the migration cannot be
   applied to old rows and the API will not start. Reset the local DB: `docker compose down -v`.
4. `FluentValidation` validators are scanned automatically with `AddValidatorsFromAssemblyContaining<Program>()`;
   adding a validator class is enough. The endpoint takes `IValidator<TRequest>` as a parameter.
5. **No comments in new code** (the project owner does not want them); comments in old files may stay.
6. Path confusion: `FlowBox.sln` is **at the repo root**, `docker-compose.yml` is inside `src/`. Do not write paths
   starting with `src/...` while the terminal is already inside `src/`.

## Tests

- `FlowBox.UnitTests` (**no Docker needed**, seconds): `Domain/` (status rules), `Service/<Feature>/` (business rules
  with fake repositories), `Validators/`, `Fakes/` (`Fake*Repository`).
- `FlowBox.IntegrationTests` (**Docker required**): `WebApplicationFactory` + Testcontainers with a real Postgres, each
  class in its own container, migrations applied from scratch. `TestAuthHelper.AuthenticateAsAsync(role)` obtains a
  token for a role through the real flow (Customer: register; Courier/Admin: the seed Admin's `/api/admin/users`).
  `CreateCourierAsync` creates a courier account and returns `courierId`; `AssignShipmentAsAdminAsync` assigns a
  shipment (a courier can only update status after the shipment is assigned to them). The factory overrides
  `AdminSeed` with test values.
- Last known state: **79 unit + 33 integration**, all green. New feature = both unit and integration tests.

```bash
dotnet test src/FlowBox.UnitTests/FlowBox.UnitTests.csproj
dotnet test src/FlowBox.IntegrationTests/FlowBox.IntegrationTests.csproj
```

## Running

```bash
cd src && docker compose up --build -d     # API: http://localhost:8080  (Scalar: /scalar/v1)
docker compose down -v                      # resets the DB (local data is deleted)
```

- Compose has a healthcheck for Postgres and the API waits for it with `condition: service_healthy`.
- Inside compose the DB host is `db`; from **outside** (Rider/DataGrip) connect to `localhost:5432` (user/password/DB
  name are in `docker-compose.yml`).
- Scalar has no JWT "Authorize" box (no security scheme was added to OpenAPI); send the
  `Authorization: Bearer <token>` header by hand with curl/Postman.

## CI and load testing

- `.github/workflows/ci.yml`: on push and PR to `main`, runs restore → build → unit → integration (using the runner's
  Docker). Every feature has its own branch + PR; merge once CI is green.
- `load-tests/tracking-lookup.js` (k6): registers its own Customer and loads `GET /api/shipments/{tn}` at 20→100→300
  VUs. Baseline: p95 ≈ 8–14 ms, 0% errors, ~125 req/s. Run: `k6 run load-tests/tracking-lookup.js`.

## Known gaps / next work

1. **`TrackingNumber` is a random 6-digit number with no unique constraint or index**: collisions are possible (the
   wrong shipment could be returned) and lookups slow down as the table grows.
2. An order created by an Admin is stored under the Admin account.
3. No refresh token, rate limiting or account lockout. JWT lifetime is 60 min.
4. No deployment (CI only verifies).
5. DataProtection keys are not persisted in the container (it logs a warning; no feature uses them today).
6. Vision items not done: Kafka/event-driven, Redis, Elasticsearch, observability, splitting into microservices,
   mobile apps, route optimization, AI prediction, external services (SMS, maps, payment).

## Checklist for a new feature

1. Open a branch (`feature/...`). 2. Entity/enum → `Data/Ef/Models`, `Enums`. 3. Repository interface + EF
implementation → register in `AddRepositories()`. 4. Service (concrete class, `: IService`) + result records.
5. Endpoint (`IEndpoint`, `RequireAuthorization`) + Validator. 6. Migration (`dotnet ef migrations add`, read the
result). 7. Unit + integration tests. 8. `dotnet test` (both), update `docs/HANDOFF.md`, commit, PR, CI green, merge.
