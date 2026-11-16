---
name: flowbox-architecture
description: >-
  FlowBox code structure: folder map, Endpoint -> Service -> Repository layering, DI registration, naming, the
  new-feature checklist and code pitfalls (CS0118, validators, no comments). Use when adding or changing an endpoint,
  service, repository, entity or validator.
---

# FlowBox — architecture

Layer folders under `src/FlowBox.Api/` are flat; inside them are feature folders: `Shipment`, `Courier`, `Order`,
`Auth`. Naming is taken from the **bubilet-panel** repo at work; copy existing files when adding new ones.

| Folder | Contents |
|---|---|
| `Endpoints/<Feature>/` | `IEndpoint` classes: HTTP only (take the request, call the Service, map the result to HTTP) |
| `Service/<Feature>/` | Business rules. `IService` is an empty marker; services are injected as **concrete classes** |
| `Repositories/<Feature>/` | `IxxxRepository` + `EfXxxRepository` (not in bubilet-panel; kept so unit tests run without a DB) |
| `Data/Ef/` | `FlowBoxDbContext` + `Models/` (entities) |
| `Domain/Shipment/` | Pure business rule: `ShipmentStatusTransitions` |
| `Validators/<Feature>/` | FluentValidation rules |
| `Extensions/` | `ClaimsPrincipalExtensions.ToCaller()` builds `Caller(UserId, Role)` from the JWT |
| `Enums/` | `ShipmentStatus`, `OrderStatus`, `UserRole` (flat, not under a module) |
| `Registry/` | `ServiceCollectionExtensions` (DI) + `EndpointRouteBuilderExtensions` (`MapEndpoints`) |
| `Migrations/` | EF migrations |

## Request flow and DI

`Endpoint (static Handle)` -> `Service` -> `IRepository` -> `EF Core`. Handlers take dependencies as **handler
parameters**, not constructors. Endpoints are discovered by reflection in `Program.cs` (`IEndpoint`, parameterless
constructor required). Services register themselves through `AddService()` (`IService`). **Repositories are not
registered automatically**: add them by hand in `AddRepositories()`.

Service methods know nothing about HTTP. They return discriminated records
(`UpdateShipmentStatusResult.Success / ShipmentNotFound / InvalidTransition`) and the endpoint maps them to status
codes with a `switch`.

## Checklist for a new feature

1. Branch (`feature/...`). 2. Entity/enum in `Data/Ef/Models`, `Enums`. 3. Repository interface + EF implementation,
registered in `AddRepositories()`. 4. Service (concrete class, `: IService`) + result records. 5. Endpoint
(`IEndpoint`, explicit `RequireAuthorization`) + Validator. 6. Migration (see `flowbox-operations`). 7. Unit +
integration tests (see `flowbox-testing`). 8. Both test suites green, update `docs/HANDOFF.md`, commit, PR.

## Pitfalls

1. **Namespace–type collision.** Feature folder names equal entity names, so a bare `Order`/`Shipment`/`Courier`
   inside `FlowBox.Api.Service.Order` gives `CS0118 'X' is a namespace but is used like a type`. Write the entity
   **fully qualified**: `Data.Ef.Models.Shipment`. (`User` does not collide; its folder is `Auth`.)
2. Validators are scanned automatically with `AddValidatorsFromAssemblyContaining<Program>()`; adding the class is
   enough. The endpoint takes `IValidator<TRequest>` as a parameter. Validation messages are in Turkish.
3. **No comments in new code**; comments in old files may stay.
4. Auth is opt-in: a new endpoint without `RequireAuthorization` is anonymous (see `flowbox-auth`).
