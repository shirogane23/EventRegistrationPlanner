# EventFlow Swagger Manual Test Plan

This runbook verifies the complete Version 1 workflow using only Swagger UI.
It follows a realistic event journey:

1. Confirm the API and database are available.
2. Configure the organizer identity in Swagger.
3. Create and manage an event.
4. Configure the attendee identity.
5. Submit and inspect a registration.
6. Return to the organizer identity to review and decide the request.
7. Exercise cancellation and event lifecycle behavior.
8. Verify authorization, validation, duplicate, capacity, and transition failures.

## 1. Start the API and open Swagger

From the repository root, start the API:

```powershell
dotnet run --project .\src\EventFlow.Api\EventFlow.Api.csproj --urls http://localhost:5200
```

Open:

```text
http://localhost:5200/swagger
```

The API must be connected to the configured SQL Server LocalDB instance:

```text
(localdb)\MSSQLLocalDB
```

Before starting the scenario, use `GET /api/health` and select **Try it out**,
then **Execute**. The expected result is `200 OK` with a healthy database
connection.

## 2. Swagger identity setup

Swagger uses the **Authorize** button at the top of the page. It is not an
application frontend login.

1. Select **Authorize**.
2. Enter `demo-organizer`.
3. Select **Authorize**, then **Close**.

The Swagger request should now contain:

```text
X-Demo-Identity: demo-organizer
```

When the scenario says to switch identity, select **Authorize**, replace the
value with the requested identity, and close the dialog again.

## 3. Organizer creates an approval-required event

Keep `demo-organizer` authorized.

Use `POST /api/events` with **Try it out** and a body like this. Choose dates
that are still in the future when running the test.

```json
{
  "title": "Community Accessibility Workshop",
  "eventType": "Workshop",
  "description": "A practical community workshop.",
  "venueId": "33333333-3333-3333-3333-333333333333",
  "startUtc": "2027-02-15T10:00:00Z",
  "endUtc": "2027-02-15T12:00:00Z",
  "registrationDeadlineUtc": "2027-02-14T23:59:00Z",
  "capacity": 2,
  "visibility": "Public",
  "approvalRequired": true
}
```

Expected:

- `201 Created`
- Response status is `Active`.
- Save the returned `eventId`.

Call `GET /api/events/owned/{eventId}` while still authorized as the organizer.
Confirm the title, venue, capacity, approval setting, and zero active
registrations.

Call `GET /api/events` and confirm the Public Active event appears in the
results.

## 4. Attendee discovers and requests registration

Switch Swagger authorization to `demo-attendee`.

Call `GET /api/events`. Confirm the event is discoverable because it is Public,
Active, in the future, and before its registration deadline.

Call `GET /api/events/{eventId}` and confirm the event details are visible.

Use `POST /api/events/{eventId}/registrations`. The request body is empty:

```json
{}
```

Expected:

- `201 Created`
- `status` is `Pending` because organizer approval is enabled.
- `confirmationReference` is `null`.
- The response contains the event and registration identifiers.

Call `GET /api/registrations`. Confirm the Pending registration appears.

**Notification check:** Pending registration must not produce a confirmation
notification. If the API console is visible, no confirmation log should appear.

## 5. Organizer reviews and approves the request

Switch Swagger authorization back to `demo-organizer`.

Call `GET /api/events/{eventId}/registrations`.

Expected:

- `200 OK`
- The guest list includes the attendee and a Pending status.

Call `POST /api/registrations/{registrationId}/approve` with the registration
identifier returned earlier. The body is empty.

Expected:

- `200 OK`
- Status changes to `Confirmed`.
- `confirmationReference` is populated with an `EF-...` value.

**Notification check:** the API console should show a confirmation notification
for the approved registration.

Switch back to `demo-attendee` and call `GET /api/registrations`. Confirm the
registration is now Confirmed and includes the confirmation reference.

## 6. Attendee cancels the registration

While authorized as `demo-attendee`, call:

`POST /api/registrations/{registrationId}/cancel`

Expected:

- `200 OK`
- Status changes to `Cancelled`.
- `DecisionReason` states that it was cancelled by the attendee.

Call `GET /api/registrations`. Cancelled registrations must not appear in the
current-registration list.

**Notification check:** the API console should show a registration-cancellation
notification.

The cancelled row remains visible to the organizer in the guest list, proving
that registration history is preserved.

## 7. Approval-required rejection flow

Create a second future event as `demo-organizer` with
`approvalRequired: true`, then switch to `demo-attendee` and register for it.

Switch back to `demo-organizer`, open its guest list, and call:

`POST /api/registrations/{registrationId}/reject`

Body:

```json
{
  "reason": "The request could not be approved for this event."
}
```

Expected:

- `200 OK`
- Status changes from Pending to `Cancelled`.
- `decisionReason` contains the supplied reason.
- `confirmationReference` remains `null`.
- A cancellation notification is logged, not a confirmation notification.

## 8. Organizer cancellation flow

Create a third future event as `demo-organizer`, register as
`demo-attendee`, and approve the registration.

As `demo-organizer`, call:

`POST /api/registrations/{registrationId}/organizer-cancel`

Body:

```json
{
  "reason": "The event program changed and this registration must be cancelled."
}
```

Expected:

- `200 OK`
- Status changes to `Cancelled`.
- The supplied reason is stored.
- The historical row remains in the organizer guest list.
- A cancellation notification is logged.

## 9. Postpone and reschedule flow

Create another future event as `demo-organizer`.

Call:

`POST /api/events/{eventId}/postpone`

Body:

```json
{
  "reason": "The venue is temporarily unavailable."
}
```

Expected:

- `200 OK`
- Status changes to `Postponed`.
- New attendee registrations return `409 Conflict`.
- The API console logs an event-postponement notification for active
  Pending/Confirmed registrations.

Call:

`POST /api/events/{eventId}/reschedule`

Body:

```json
{
  "startUtc": "2027-03-15T10:00:00Z",
  "endUtc": "2027-03-15T12:00:00Z",
  "registrationDeadlineUtc": "2027-03-14T23:59:00Z",
  "venueId": "33333333-3333-3333-3333-333333333333"
}
```

Expected:

- `200 OK`
- Status returns to `Active`.
- The new schedule is returned.
- The API console logs a reschedule notification.

## 10. Close and cancel flow

Create a separate future event as `demo-organizer`.

Call `POST /api/events/{eventId}/close` with an empty body.

Expected:

- `200 OK`
- Status becomes `Closed`.
- New registration returns `409 Conflict`.
- Attempting to reschedule the Closed event returns `409 Conflict`.
- Closed is final.

Create another separate future event and call:

`POST /api/events/{eventId}/cancel`

Body:

```json
{
  "reason": "The event will not proceed."
}
```

Expected:

- `200 OK`
- Status becomes `Cancelled`.
- New registration returns `409 Conflict`.
- Repeating cancellation returns `409 Conflict`.
- The API console logs event-cancellation notifications for active
  registrations.

## 11. Authorization and validation failures

Run these checks through Swagger and confirm the exact status category:

| Test | Swagger action | Expected |
|---|---|---|
| Missing identity | Click **Authorize**, clear the value, call a protected endpoint | `401 Unauthorized` |
| Unknown identity | Enter `unknown-user`, call a protected endpoint | `401 Unauthorized` |
| Wrong role | Enter `demo-attendee`, call `POST /api/events` | `403 Forbidden` |
| Wrong owner | Use an organizer identity that does not own the event | `403 Forbidden` |
| Duplicate registration | Register the same attendee for the same event twice | `409 Conflict` |
| Re-register after cancellation | Register again after attendee cancellation | `409 Conflict` |
| Approve Confirmed | Approve the same registration twice | `409 Conflict` |
| Reject Confirmed | Reject an already Confirmed registration | `409 Conflict` |
| Cancel Cancelled | Cancel a Cancelled registration | `409 Conflict` |
| Missing action reason | Reject/cancel with `{}` | `400 Bad Request` |
| Invalid event dates | Create with end before start or deadline after start | `400 Bad Request` |
| Capacity over venue limit | Create with capacity greater than 50 for venue `333...` | `409 Conflict` |
| Unknown venue | Create with a random venue ID | `404 Not Found` |
| Invalid reschedule state | Reschedule Active, Closed, or Cancelled event | `409 Conflict` |
| Overlapping venue schedule | Create two Active events overlapping at the same venue | `409 Conflict` |

For every failure, confirm the response is Problem Details JSON and that no
unexpected state change occurred by repeating the relevant GET request.

## 12. Capacity verification

The seeded database contains one attendee identity:

```text
demo-attendee
```

Therefore Swagger alone can verify that one Pending or Confirmed registration
counts toward capacity, and that a duplicate attempt is rejected. To test two
different attendees competing for the final slot, the database must contain a
second seeded attendee identity. Adding that identity is database/setup work,
not a Swagger operation, because Version 1 intentionally has no user-creation
endpoint.

With a second approved attendee available, use an event with `capacity: 1`:

1. Attendee A registers successfully.
2. Attendee B registers for the same event.
3. Expect `409 Conflict`.
4. Cancel Attendee A's registration.
5. Attendee B can then register, unless the test is specifically verifying the
   Version 1 no-reregistration rule for Attendee A.

## 13. Final pass criteria

The Swagger-only acceptance test passes when:

- Health and database connectivity return `200`.
- Swagger **Authorize** sends `X-Demo-Identity`.
- Organizer and attendee permissions are enforced.
- Public search and direct event details behave correctly.
- Registration creates Pending or Confirmed according to approval settings.
- Pending never receives a confirmation notification.
- Approval creates a confirmation reference and notification.
- Attendee and organizer cancellations preserve history and release capacity.
- Rejection requires a reason and stores it.
- Close, postpone, reschedule, and cancel enforce valid transitions.
- Venue capacity and overlap rules are enforced.
- Duplicate, lifecycle, ownership, identity, and validation failures return the
  documented status codes.
- No unexpected database state changes occur after a failed request.

After completing the runbook, close the API process and record any failing
endpoint, request body, response status, response body, and event or
registration identifier for troubleshooting.
