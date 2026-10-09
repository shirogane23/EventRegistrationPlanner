# Phase 6: attendee registration services

Phase 6 adds attendee registration, current-registration reads, cancellation,
duplicate prevention, and transaction-safe capacity reservation.

## Endpoints

### Create a registration

```text
POST /api/events/{eventId}/registrations
X-Demo-Identity: demo-attendee
```

The request body is intentionally empty. Identity, status, timestamps, and
confirmation reference are server-owned.

An eligible event produces:

- `Confirmed` when approval is disabled, with a confirmation reference.
- `Pending` when approval is enabled, without a confirmation reference.

### List current registrations

```text
GET /api/registrations
X-Demo-Identity: demo-attendee
```

Only Pending and Confirmed registrations are returned. Cancelled historical
rows remain in the database but are not part of the current attendee view.

### Cancel a registration

```text
POST /api/registrations/{registrationId}/cancel
X-Demo-Identity: demo-attendee
```

Only the registration owner may cancel it. Cancellation changes the row to
Cancelled and releases capacity without deleting history.

## Registration rules

- The caller must be a seeded Attendee.
- The event must be Active, before its deadline, before its start, and below
  capacity.
- Pending and Confirmed registrations both reserve capacity.
- A user/event pair can have only one registration attempt.
- A user cannot register again after cancellation.
- Only Pending or Confirmed registrations can be cancelled.
- Event cancellation prevents attendee cancellation through this operation.

## Concurrency

Registration creation runs in a SQL Server Serializable transaction. The
eligibility check, active-registration count, and insert are performed within
the same transaction so concurrent requests cannot intentionally claim more
than the available capacity. The database unique user/event constraint remains
the final duplicate safeguard.

## Phase 6 verification

```powershell
dotnet build .\EventFlow.sln
dotnet run --project ".\src\EventFlow.Api\EventFlow.Api.csproj"
```

Use an event created in Phase 5 and test both seeded identities. Verify
Confirmed versus Pending status, duplicate `409` responses, current-list
filtering, owner checks, and capacity release after cancellation.
