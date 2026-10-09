# Revised EventFlow implementation plan

## Problem and approach

The proposal defines a feasible Swagger-only event-registration backend, but it
does not yet connect the agreed product rules to the required EF Core,
DTO/AutoMapper, REST CRUD, testing, ERD, and use-case deliverables. The
repository has no implementation yet. This plan closes those gaps while
preserving the requested database-first workflow:

1. The team creates and approves the SQL Server schema and ERD first.
2. The team runs `Scaffold-DbContext` against that existing database to
   generate the EF Core models and `DbContext`.
3. Application code adds DTOs, AutoMapper profiles, validation, services, and
   controllers around the generated persistence layer.
4. Swagger demonstrates the API and documents the deliberate Version 1
   identity limitation.

The plan does **not** create the database, write EF entity classes, or write a
`DbContext`; those are outputs of the team's schema and scaffolding steps.

## Confirmed Version 1 scope

- Two conceptual roles: attendee and organizer; no administrator.
- Free, in-person, public and unlisted events; Swagger is the only interface.
- Events are immediately active after valid creation; no draft workflow.
- Event lifecycle: Active, Closed, Postponed, and Cancelled.
- Registration lifecycle: Pending, Confirmed, and Cancelled.
- Approval-off registrations confirm immediately; approval-on registrations
  reserve capacity as Pending until organizer approval.
- No waitlist, payments, tickets, QR/check-in, attendance, reminders, or
  past-registration UI/workflow.
- Organizer rejection is represented as Cancelled with a reason.
- A cancelled attendee cannot re-register for the same event in Version 1.
- Closed events cannot reopen; postponed events may be rescheduled and
  reactivated by the organizer.
- Organizers may cancel registrations for events they own.
- Historical rows remain in the database but are not exposed as a past
  registrations feature.
- Swagger has no authentication in Version 1. Actor identity and role are a
  documented demonstration limitation, not a security boundary.

## Todos

1. **Finalize business decisions and acceptance rules**
   - Convert the proposal's agreed/recommended rules into an acceptance
     matrix, including event lifecycle transitions, registration transitions,
     deadline behavior, cancellation rules, visibility, and ownership checks.
   - Explicitly document the no-auth Swagger limitation and prevent claims of
     production-grade authorization.

2. **Design and approve the database/ERD before scaffolding**
   - Produce the ERD and SQL Server schema for User/Actor, Venue, Event, and
     Registration, including primary keys, foreign keys, required fields,
     unique constraints, status representation, timestamps, confirmation
     reference, cancellation/rejection reason, and ownership.
   - Define indexes and constraints for event search, duplicate active
     registrations, visibility, lifecycle filtering, and venue scheduling.
   - Decide how the initial organizer accounts and permitted venues are seeded.
   - Define a concurrency-safe capacity strategy so Pending plus Confirmed
     registrations can never exceed capacity.
   - Keep schema creation separate from EF code generation; do not use EF
     migrations as a substitute for the database-first source schema.

3. **Scaffold and configure EF Core from the approved database**
   - Add the ASP.NET Core Web API project and required EF Core SQL Server,
     design-time, AutoMapper, and Swagger packages.
   - Run the team's `Scaffold-DbContext` command against the existing SQL
     Server database, using `Data` for the context and `Models` for generated
     entities, and exclude `OnConfiguring` as appropriate.
   - Register the generated context through configuration and dependency
     injection; keep the connection string out of source control.
   - Treat generated files as generated artifacts and avoid putting business
     rules inside them.

4. **Add DTO, mapping, validation, and service boundaries**
   - Define request DTOs for event creation/update, event search/filtering,
     registration, organizer approval/rejection, attendee cancellation,
     organizer cancellation, close, postpone, reschedule, and event cancel.
   - Define response DTOs so controllers never expose scaffolded entities.
   - Add AutoMapper profiles for every entity-to-DTO and request-to-entity
     mapping; map server-owned fields explicitly.
   - Add model binding and validation for dates, deadline ordering, capacity,
     venue capacity, required fields, visibility, and allowed enum/status
     values.
   - Put lifecycle, ownership, duplicate-registration, capacity, and
     concurrency rules in application services rather than controllers.
   - Return consistent validation, not-found, conflict, forbidden-by-demo-rule,
     and invalid-transition responses.

5. **Implement the RESTful CRUD and workflow API**
   - Provide event create/read/update/delete behavior with ownership and
     lifecycle restrictions; deletion must preserve registration history or be
     replaced by a documented cancellation policy.
   - Provide public event listing/search by partial title, event details, and
     unlisted-by-identifier access without normal search discovery.
   - Provide attendee registration, current-registration lookup, and
     cancellation.
   - Provide organizer-owned event listing, registrant listing, approval,
     rejection-as-cancellation, organizer cancellation, close, postpone,
     reschedule/reactivation, and event cancellation.
   - Use proper HTTP methods and status codes, including `201 Created`,
     `200/204`, `400`, `404`, `409`, and `403` where applicable.
   - Make email a replaceable notification service. Implement the accepted
     confirmation and event-change triggers using a development-safe provider
     or explicit stub, clearly reporting notification failure without
     pretending delivery is guaranteed.

6. **Document Swagger testing and use cases**
   - Configure Swagger schemas, operation summaries, response examples,
     actor/role demonstration instructions, and representative error
     responses.
   - Create an endpoint test checklist or collection covering happy paths,
     invalid model binding, ownership boundaries, duplicate attempts,
     capacity races, lifecycle transitions, cancellation, visibility, and
     notification triggers.
   - Demonstrate at least these scenarios: immediate confirmation, approval
     flow, organizer rejection, cancellation freeing capacity, full event,
     postponed/rescheduled event, cancelled event, and unlisted discovery.
   - Include an ERD with table keys and relationships, a source-tree
     explanation, and use-case narratives with actor, preconditions, main flow,
     rules, and expected result.

7. **Final gap review and acceptance**
   - Verify every mandatory component and deliverable against a traceability
     matrix: database-first schema/scaffold, EF Core, DTOs, AutoMapper, model
     binding, REST CRUD, working API, ERD, source code, Swagger testing, and
     use cases.
   - Verify excluded features have not re-entered the design.
   - Run build, API tests, and a clean database/scaffold verification before
     presenting the implementation.

## Gaps and risks closed by this revision

- **Identity and authorization gap:** no Admin exists and no Swagger identity
  mechanism was selected; the plan now labels this as a deliberate demo
  limitation and requires consistent actor simulation/documentation.
- **Database-first gap:** the proposal only names conceptual entities; the plan
  requires an approved ERD/schema before scaffolding and explicitly forbids
  hand-authoring generated EF persistence code.
- **Concurrency gap:** capacity was specified functionally but lacked a
  transaction/constraint strategy; schema indexes and an atomic service
  operation are now required.
- **CRUD/API gap:** role activities were not endpoint contracts; the plan
  requires complete CRUD plus workflow operations and HTTP response semantics.
- **DTO/AutoMapper/model-binding gap:** these mandatory rubric items were absent
  from the proposal and are now explicit application layers.
- **Delete/history contradiction:** physical deletion would conflict with
  retaining historical registrations; event deletion is therefore constrained
  by a documented cancellation/history policy.
- **Email gap:** triggers were listed without a provider, failure behavior, or
  delivery caveat; the plan requires a replaceable, development-safe notifier
  and honest delivery semantics.
- **Testing/deliverable gap:** Swagger was named but no test matrix, ERD
  artifact, source-structure explanation, or use-case format was defined.
- **Configuration gap:** connection strings, seeded demo data, and generated
  code ownership are now explicit setup/documentation concerns.

## Notes and boundaries

- The schema is the source of truth; EF scaffolding is rerunnable after schema
  changes and generated code should not be manually redesigned.
- No custom frontend, authentication system, admin module, migration-first
  workflow, or out-of-scope event features should be added.
- If the team later requires real role enforcement, that is a separate scope
  decision and should not be silently simulated as secure authorization.
