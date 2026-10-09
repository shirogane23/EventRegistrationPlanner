# EventFlow
## Final System Planning and Scope Paper

**Document status:** Final planning baseline for team approval
**Project type:** Backend-only ASP.NET Core Web API
**Database:** SQL Server, database-first
**Interaction and demonstration:** Swagger/OpenAPI
**Revision:** October 2026

## 1. Executive summary

EventFlow is a registration-management API for free, in-person events. Attendees discover eligible events, submit registrations, view current registration status, and cancel when permitted. Organizers create and manage their own events, control capacity, review registration requests, and close, postpone, reschedule, or cancel events.

The system manages registration, not physical admission. A confirmation email proves only that EventFlow confirmed a registration. It does not prove identity, attendance, or entry at the venue. Check-in, QR scanning, attendance recording, payment, and ticketing are outside the system boundary.

The implementation must use a database-first approach. The team will approve the SQL Server schema and ERD first, then reverse-engineer the database with EF Core `Scaffold-DbContext`. Generated entities and the generated `DbContext` will be used by the API, while DTOs, AutoMapper, model binding, validation, services, and controllers provide the application boundary.

## 2. Objectives and success criteria

EventFlow is successful when it provides:

1. A working ASP.NET Core Web API connected to SQL Server.
2. EF Core entities and `DbContext` generated from the approved existing database.
3. DTO request/response contracts that never expose database entities directly.
4. AutoMapper profiles for controlled entity/DTO mapping.
5. Validated model binding for incoming requests.
6. RESTful Create, Read, Update, and Delete behavior, using safe lifecycle semantics where physical deletion would destroy history.
7. Correct capacity, ownership, duplicate-registration, and event-lifecycle rules.
8. Swagger demonstrations for normal and failure cases.
9. An ERD and use-case documentation that match the implemented system.

## 3. Users and authorization

### 3.1 Attendee

An attendee can:

- Search public events by partial title.
- View public event details and access an unlisted event through its direct identifier.
- Register for an eligible event.
- View current registrations and their status.
- Cancel an eligible Pending or Confirmed registration.

### 3.2 Organizer

An organizer can:

- Create events using permitted venues.
- Read and update events that they own.
- View registration lists and capacity summaries for owned events.
- Approve or reject Pending requests when approval is enabled.
- Cancel a registration for an owned event with a reason.
- Close, postpone, reschedule/reactivate, or cancel an owned event.

### 3.3 Identity decision

Version 1 uses seeded demo users. Swagger requests supply an explicitly documented demo-identity header or claim. The API resolves that identity to a seeded user and role, then applies centralized ownership and authorization checks.

This mechanism is an academic demonstration seam, not production authentication. The documentation must state that it does not provide password security, token integrity, account recovery, or real-world identity verification.

There is no administrator role. Ordinary users cannot create organizer accounts or venues.

## 4. Scope decisions

### Included

- Free, physical events.
- Public and unlisted visibility.
- Optional per-event organizer approval.
- Capacity shared by Pending and Confirmed registrations.
- Registration cancellation and capacity release.
- Event Active, Closed, Postponed, and Cancelled lifecycle states.
- Partial event-title search.
- Confirmation, postponement/reschedule, and cancellation emails.
- Internal preservation of historical registrations.
- Swagger-based API demonstration.

### Excluded

- Administrator dashboards or moderation.
- Draft events and a separate publishing workflow.
- QR codes, scanning, check-in, admission verification, or attendance.
- Payments, refunds, pricing, tickets, or seat selection.
- Waitlists, expiring holds, or automatic queue promotion.
- Invitation-only access management.
- Website, mobile application, or scanner application.
- Past-registration browsing in the attendee experience.
- Attendance reports, reminders, campaigns, or an email retry dashboard.
- Recommendation engines or natural-language search.

## 5. Event lifecycle

| State | Meaning | New registration | Existing registrations |
|---|---|---|---|
| Active | Event is scheduled and available when normal rules pass | Allowed while eligible and capacity remains | Continue normally |
| Closed | Organizer permanently stops new registration | Blocked | Preserved; Pending may be approved before event start |
| Postponed | Original schedule is paused while a replacement schedule is prepared | Blocked | Preserved; affected users are notified |
| Cancelled | Event will not proceed | Blocked | Preserved; affected users are notified |

Closed is final in Version 1 and cannot be reopened. A Postponed event may be rescheduled by its owner with a valid future start/end time, deadline, venue-capacity check, and conflict check. Once approved, it returns to Active and existing registrations carry over.

An event whose deadline has passed or whose start time has arrived cannot accept new registrations. A Pending request already created before the deadline may be approved after the deadline, but never at or after the event start.

## 6. Registration lifecycle

Only three registration statuses are used:

- **Pending:** approval is required; the registration reserves capacity.
- **Confirmed:** registration is approved or automatically confirmed; it reserves capacity.
- **Cancelled:** registration is no longer active; it does not reserve capacity.

With approval disabled, an eligible registration becomes Confirmed immediately. With approval enabled, it becomes Pending and the organizer later approves or rejects it. Rejection is represented as Cancelled and requires a reason. Organizers may cancel Pending or Confirmed registrations for their own events and must provide a reason.

Version 1 permits one registration attempt per user/event. A user cannot register again after cancellation. Historical rows remain stored rather than being deleted.

Attendees may cancel Pending or Confirmed registrations while the event has not been cancelled and the cancellation policy permits it. Cancellation releases capacity but never reopens a Closed event or overrides a Postponed/Cancelled state.

## 7. Business rules and validation

### Event rules

- Title, type, description, venue, schedule, deadline, capacity, visibility, and approval setting are required according to the approved schema.
- Start time must be in the future at creation.
- End time must be after start time.
- Registration deadline must not be after start time.
- Capacity must be greater than zero and no greater than venue capacity.
- Active events at the same venue cannot overlap.
- Capacity cannot be reduced below the number of Pending plus Confirmed registrations.
- Significant schedule or venue changes use the postponement/rescheduling flow rather than an ordinary edit.
- Only the owner may modify or transition an event.

### Registration rules

- The event must be Active, before its deadline, before its start, and below capacity.
- Pending and Confirmed count toward capacity.
- Cancelled registrations do not count.
- A duplicate user/event registration is rejected.
- Capacity checks and registration creation must be atomic so concurrent requests cannot exceed capacity.
- A registration request must not expose or accept server-owned status, user identity, confirmation reference, or timestamps from the client.

### Model binding and error behavior

Incoming DTOs are validated using the projectâ€™s standard ASP.NET Core validation mechanism. The API returns consistent Problem Details responses:

| Condition | HTTP response |
|---|---|
| Valid creation | `201 Created` |
| Valid read/update | `200 OK` |
| Successful state change or safe delete | `200 OK` or `204 No Content`, documented per operation |
| Invalid DTO or missing required value | `400 Bad Request` |
| Missing/invalid demo identity | `401 Unauthorized` |
| Valid identity without permission | `403 Forbidden` |
| Resource not found | `404 Not Found` |
| Duplicate or capacity/lifecycle conflict | `409 Conflict` |

## 8. Conceptual data model and ERD baseline

The following is the logical ERD baseline. The final SQL schema, field definitions, constraints, and indexes must be approved before scaffolding.

```text
User 1 â”€â”€â”€â”€â”€â”€â”€< Event >â”€â”€â”€â”€â”€â”€â”€ 1 Venue
  â”‚              â”‚
  â”‚              â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€< Registration >â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€<
```

### Entities and relationships

| Entity | Purpose | Relationships |
|---|---|---|
| User | Seeded attendee/organizer identity, email, role | One organizer owns many events; one user has many registrations |
| Venue | Permitted physical location and maximum capacity | One venue hosts many events |
| Event | Event details, owner, venue, schedule, visibility, approval, capacity, lifecycle state | Belongs to one organizer and venue; has many registrations |
| Registration | User/event association, status, timestamps, reference, and decision reason | Belongs to one user and one event |

The database design must define:

- Primary keys and foreign keys.
- Required and optional columns.
- Unique user/event registration rule.
- Indexes for title search, event ownership, event status/schedule, and registration lookup.
- Restrict/cascade behavior that preserves registration history.
- UTC timestamp storage.
- Representation of role, visibility, lifecycle, and registration statuses.
- Safe seed data for permitted venues and demo identities.

Cross-row rules such as overlapping venue schedules and atomic capacity reservation may require API transaction logic in addition to database constraints.

## 9. Database-first and EF Core implementation path

The implementation order is intentionally database-first:

1. Approve this scope and the logical ERD.
2. Create and validate the SQL Server database/schema and seed data.
3. Create the ASP.NET Core Web API project and install the approved EF Core SQL Server/design/tooling packages.
4. Reverse-engineer the approved database, for example:

```powershell
Scaffold-DbContext "Server=localhost\SQLEXPRESS;Database=EventFlowDb;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -Context EventFlowDbContext -ContextDir Data -OutputDir Models -NoOnConfiguring
```

The actual connection string, database name, table selection, and context name must match the approved environment. The command is an implementation example, not a database design.

5. Verify generated relationships, requiredness, indexes, and naming against the ERD.
6. Register the generated `DbContext` using external configuration; do not embed the connection string in generated code.
7. Keep generated entity changes separate from API contracts. If the database changes, update the schema and re-scaffold rather than hand-designing replacement entities.

## 10. DTO, AutoMapper, and REST API design requirements

The API must define request DTOs for:

- Event creation and permitted ordinary updates.
- Registration creation and attendee cancellation.
- Organizer approval, rejection, and organizer cancellation.
- Event close, postpone, reschedule/reactivate, and cancellation actions.

It must define response DTOs for:

- Public event search and details.
- Current attendee registrations.
- Organizer event details and registration summaries.
- Organizer guest lists.
- Standard Problem Details errors.

AutoMapper profiles must map generated entities to response DTOs and request DTOs to allowed entity fields. Server-owned valuesâ€”including identity, event ownership, status, timestamps, capacity counts, and confirmation referencesâ€”must not be client-overwritable.

Controllers should use meaningful REST conventions and delegate lifecycle/capacity/notification behavior to application services rather than embedding complex state logic in actions. Venue data is read-only to ordinary users and maintained through seed/setup data in Version 1.

## 11. Notification behavior

The notification boundary is intentionally small:

| Trigger | Recipients | Required content |
|---|---|---|
| Automatic confirmation | Newly Confirmed attendee | Name, event, schedule, venue, reference, status |
| Organizer approval | Newly Confirmed attendee | Same confirmation details |
| Postponement | Pending and Confirmed registrants | Postponement notice and schedule status |
| Reschedule/reactivation | Affected registrants | New schedule and venue if changed |
| Event cancellation | Pending and Confirmed registrants | Clear cancellation and status-specific wording |

The system must never send a confirmation message for a Pending registration. Email delivery is external and is not guaranteed. Provider configuration, failure reporting, and transaction ordering must be documented. No retry dashboard or delivery-history module is part of Version 1.

## 12. Use cases

### UC-01: Attendee registers for an approval-free event

**Preconditions:** seeded attendee identity; event is Active, visible, before deadline/start, and has capacity.
**Action:** attendee submits a registration DTO.
**Rules:** reject duplicate registration; reserve capacity atomically.
**Result:** create Confirmed registration and send confirmation notification.
**Failure:** return `409` for duplicate/full/ineligible registration.

### UC-02: Attendee requests an approval-required event

**Preconditions:** same eligibility rules; event approval is enabled.
**Action:** attendee submits registration.
**Result:** create Pending registration, reserve capacity, and do not send a confirmation email.
**Failure:** return `409` for duplicate/full/ineligible registration.

### UC-03: Organizer approves a request

**Preconditions:** organizer owns the event; registration is Pending; event has not started.
**Action:** organizer approves the request.
**Result:** transition to Confirmed and send confirmation details.
**Failure:** return `403` for another organizerâ€™s event or `409` for an invalid transition.

### UC-04: Organizer rejects or cancels a request

**Preconditions:** organizer owns the event; registration is Pending or Confirmed.
**Action:** organizer submits a reason.
**Result:** transition to Cancelled and release capacity; notify the affected user when required.

### UC-05: Attendee cancels

**Preconditions:** attendee owns the registration and it is Pending or Confirmed.
**Action:** submit cancellation.
**Result:** transition to Cancelled and release capacity without reopening a Closed event.

### UC-06: Organizer postpones and reschedules

**Preconditions:** organizer owns an event that has not been cancelled or completed.
**Action:** postpone, then submit a valid future schedule and deadline.
**Result:** block new registrations while postponed; after reactivation, carry over registrations and notify affected users.
**Rules:** validate venue conflicts, venue capacity, date ordering, and registration count.

### UC-07: Organizer cancels an event

**Preconditions:** organizer owns the event.
**Action:** cancel the event.
**Result:** block new registrations, preserve all registration rows, and notify Pending and Confirmed registrants with accurate wording.

## 13. Verification and deliverables

### API testing

Tests must cover:

- DTO validation and overposting protection.
- Demo identity, role, and ownership boundaries.
- Event Create/Read/Update and safe lifecycle behavior.
- Registration creation, duplicate prevention, and cancellation.
- Approval, rejection, organizer cancellation, and invalid transitions.
- Public versus unlisted visibility and title search.
- Capacity boundaries and venue-capacity validation.
- Concurrent requests for the final available slot.
- Postponement/rescheduling and event cancellation notifications.

### Swagger demonstration

Swagger must show:

- Seeded demo identity instructions.
- Request and response DTO examples.
- Success paths for attendee and organizer scenarios.
- Validation, authorization, duplicate, full-capacity, and lifecycle-conflict responses.
- The distinction between Pending and Confirmed email behavior.

### Final submission package

1. Working backend API source code.
2. Approved SQL Server schema or setup script.
3. ERD showing tables, primary keys, foreign keys, and cardinalities.
4. Scaffolded EF Core entities and `DbContext`.
5. DTOs, AutoMapper profiles, validation, services, and controllers.
6. Swagger/OpenAPI demonstration.
7. Automated API tests and documented results.
8. This use-case and business-rules paper.
9. Explicit out-of-scope register.

## 14. Final approval checklist

Before implementation begins, the group must confirm:

- The seeded demo-identity mechanism is acceptable for the academic demonstration.
- Closed events are final and Postponed events can be rescheduled/reactivated.
- Rejection and organizer cancellation use Cancelled plus a required reason.
- Re-registration after cancellation is disallowed in Version 1.
- UTC timestamps and deadline/start cutoffs are accepted.
- Pending and Confirmed both reserve capacity.
- Registration history is preserved and ordinary deletes do not destroy it.
- Email failure behavior and non-guaranteed delivery wording are accepted.
- The ERD and SQL schema will be approved before `Scaffold-DbContext` is run.
- The excluded features will not be reintroduced during implementation.

Once these conditions are approved, implementation can proceed in the order: database and ERD, EF Core scaffolding, DTOs and mappings, API behavior, notifications, tests, and Swagger evidence.
