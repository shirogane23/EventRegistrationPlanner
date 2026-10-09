# Phase 3: DTOs, validation, and mappings

Phase 3 defines the API contract boundary between controllers and the
scaffolded EF Core entities. Controllers and services in later phases must
accept request DTOs and return response DTOs; generated database entities must
not be exposed directly from HTTP endpoints.

## Request contracts

- `EventCreateRequest` accepts event content, venue, schedule, capacity,
  visibility, and approval configuration.
- `EventUpdateRequest` permits ordinary content edits only.
- `EventPostponeRequest` and `EventCancelRequest` require a reason.
- `EventRescheduleRequest` validates a future schedule and optional venue.
- `RegistrationCreateRequest` intentionally has no writable properties. The
  identity and event identifier come from request context and route.
- `RegistrationActionRequest` requires a reason for organizer rejection,
  organizer cancellation, or other reason-bearing state changes.

Ownership, lifecycle status, timestamps, registration counts, and confirmation
data are not accepted from clients.

## Validation behavior

ASP.NET Core `[ApiController]` model binding returns `400 Bad Request` for
invalid DTOs. The event contracts validate required values, database-aligned
length limits, future start time, schedule ordering, positive capacity, and
allowed visibility. Lifecycle actions validate required reasons.

Cross-row checks such as venue capacity, overlapping events, and registration
capacity remain service/transaction rules for later phases.

## AutoMapper

`EventFlowMappingProfile` is registered from `Program.cs`. It maps allowed
event creation and update fields to entities, and maps events, venues, and
registrations to response DTOs.

Create/update maps explicitly ignore server-owned and navigation properties.
This prevents a future controller from accidentally allowing overposting when
mapping request DTOs onto generated entities.

## Phase 3 completion criteria

- DTOs compile with database-aligned length and requiredness rules.
- AutoMapper is registered and the profile can validate at startup/test time.
- Response DTOs expose API contracts rather than EF entities.
- Server-owned fields cannot be populated by event or registration request
  DTOs.
- Lifecycle and organizer action contracts exist for later service/controller
  implementation.
