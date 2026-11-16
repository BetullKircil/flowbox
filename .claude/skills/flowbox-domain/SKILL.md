---
name: flowbox-domain
description: >-
  FlowBox domain model: Order, Shipment, ShipmentTrackingEvent, ShipmentAssignment, User and Courier, plus the
  shipment status pipeline and its transition rules. Use when touching entities, tracking numbers, status changes or
  courier assignment.
---

# FlowBox — domain

- **Order**: the customer's request, carries `CustomerId` (-> `User`). Today it becomes a Shipment synchronously when
  created (1:1). The split will pay off once steps such as payment confirmation are inserted between them.
- **Shipment**: the real tracking unit (`TrackingNumber`, `OrderId`). The tracking number is generated in
  `OrderService` (format `TR` + digits).
- **ShipmentTrackingEvent**: one record per status change; never deleted or updated.
- **ShipmentAssignment**: courier assignment history. Only the active assignment grants the courier access.
- **User** (unique Email, PasswordHash, Role: Customer/Courier/Admin) and **Courier** (Name, Phone) are linked:
  `Courier.UserId` is required and unique. Creating a Courier account supplies the profile too;
  `AuthService.RegisterAsync` creates both in one operation. A Courier account without a profile cannot exist
  (`RegisterResult.CourierProfileRequired`).

## Status pipeline

`Created -> PickedUp -> ArrivedAtSortingCenter -> Sorted -> InTransit -> ArrivedAtDistributionCenter ->
OutForDelivery -> Delivered`

- `Failed` is reachable from every non-terminal state.
- Steps cannot be skipped; nothing can change after `Delivered` or `Failed`.
- The rule lives in `Domain/Shipment/ShipmentStatusTransitions`; the service returns `InvalidTransition` when it fails.

For who may read or change a shipment, see `flowbox-auth`.
