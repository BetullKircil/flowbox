---
name: flowbox-auth
description: >-
  FlowBox authentication and authorization: JWT setup, roles, the endpoint permission table, shipment ownership
  (404 rule), AdminSeed and the opt-in RequireAuthorization trap. Use when adding or protecting an endpoint, changing
  roles or ownership, or debugging 401/403/404.
---

# FlowBox — auth

## Endpoints and who may call them

| Endpoint | Who |
|---|---|
| `POST /api/auth/register` | Everyone (always creates a **Customer**; a `role` in the body is ignored) |
| `POST /api/auth/login` | Everyone |
| `POST /api/admin/users` | Admin (creates Customer/Courier/Admin; Courier requires `name` and `phone`, response returns `courierId`) |
| `POST /api/orders`, `GET /api/orders/mine` | Customer, Admin |
| `GET /api/shipments/{tn}`, `/tracking`, `/history` | Authenticated + **ownership** (below) |
| `PATCH /api/shipments/{tn}/status` | Courier (same ownership rule), Admin |
| `GET /api/couriers/me/shipments` | Courier (actively assigned shipments, paged) |
| `PATCH /api/shipments/{tn}/assign`, `GET /api/shipments` | Admin |

## Ownership

- Admin sees every shipment; Customer only the shipment of their own order; Courier only a shipment **actively
  assigned** to them. Everyone else gets **404**, not 403, so existence is not leaked.
- Role rules go on the endpoint: `.RequireAuthorization(policy => policy.RequireRole(...))`. Ownership rules live in
  the Service (`ShipmentService.CanAccessAsync`) using `Caller(UserId, Role)` from `ToCaller()`.
- Reassigning a shipment removes the previous courier's access.
- Auth is **opt-in**: forgetting `RequireAuthorization` on a new endpoint makes it anonymous.

## JWT and seed

- HS256, role claim is `ClaimTypes.Role`. `Jwt:Issuer/Audience/ExpiryMinutes` are in `appsettings.json`; **an empty
  `Jwt:Secret` stops the app at startup**. Dev secret is in `appsettings.Development.json`; production uses the
  `Jwt__Secret` environment variable. Never print these values.
- **First Admin**: only an Admin can create an Admin, so `AdminSeed:Email/Password` creates one at startup
  (idempotent, never changes an existing user's role). Dev values are in `appsettings.Development.json`; production
  uses `AdminSeed__Email` / `AdminSeed__Password`.
- A wrong password and an unknown email return the same 401, on purpose.
- Scalar has no JWT "Authorize" box; send `Authorization: Bearer <token>` by hand with curl/Postman.
