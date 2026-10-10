# EventFlow Swagger Manual Test Plan: Conference Seed

This is an alternate Swagger-only acceptance scenario using a different event
seed from the workshop scenario in `Swagger_Manual_Test_Plan.md`.

The workflow models a large public conference:

1. The organizer creates a conference using the large seeded venue.
2. The attendee discovers and requests access.
3. The organizer reviews and approves the request.
4. The attendee verifies confirmation and later cancels.
5. The organizer postpones, reschedules, closes, and cancels separate event
   records to verify lifecycle behavior.

This plan does not modify database seed data. It uses new event records and the
existing seeded identities and venues:

```text
Organizer: demo-organizer
Attendee:  demo-attendee
Large venue: 44444444-4444-4444-4444-444444444444
Small venue: 33333333-3333-3333-3333-333333333333
```

Choose dates in the future when executing the plan.

## 1. Start the API and configure Swagger

From the repository root:

```powershell
dotnet run --project .\src\EventFlow.Api\EventFlow.Api.csproj --urls http://localhost:5200
```

Open:

```text
http://localhost:5200/swagger
```

Use **Authorize** and enter:

```text
demo-organizer
```

The Swagger requests should include:

```text
X-Demo-Identity: demo-organizer
```

## 2. Check system health

Execute `GET /api/health`.

Expected:

```text
200 OK
status: Healthy
database: Connected
```

Execute `GET /api/identity/me`.

Expected:

```text
200 OK
role: Organizer
```

## 3. Seed a conference event

As `demo-organizer`, execute `POST /api/events`:

```json
{
  "title": "Regional Technology and Innovation Conference",
  "eventType": "Conference",
  "description": "A full-day conference for technology and innovation leaders.",
  "venueId": "44444444-4444-4444-4444-444444444444",
  "startUtc": "2027-05-20T08:30:00Z",
  "endUtc": "2027-05-20T17:30:00Z",
  "registrationDeadlineUtc": "2027-05-19T23:59:00Z",
  "capacity": 300,
  "visibility": "Public",
  "approvalRequired": true
}
```

Expected:

- `201 Created`
- Status is `Active`.
- Capacity is `300`, which fits the large venue.

Save the returned `eventId` as `conferenceEventId`.

Execute `GET /api/events/owned/{conferenceEventId}` and verify the conference
details, large venue, approval setting, and zero active registrations.

## 4. Attendee discovers the conference

Change Swagger **Authorize** to:

```text
demo-attendee
```

Execute `GET /api/identity/me`.

Expected role:

```text
Attendee
```

Execute `GET /api/events?title=Technology`.

Expected:

- The conference appears in the results.
- The result is Public, Active, future-dated, and before its deadline.

Execute `GET /api/events/{conferenceEventId}` and confirm the full conference
details.

## 5. Attendee submits a Pending registration

Execute `POST /api/events/{conferenceEventId}/registrations` with:

```json
{}
```

Expected:

- `201 Created`
- Status is `Pending`.
- `confirmationReference` is `null`.

Save the returned `registrationId` as `conferenceRegistrationId`.

Execute `GET /api/registrations`.

Confirm the conference appears as a current Pending registration.

Check the API console: there must not be a confirmation notification for this
Pending registration.

## 6. Organizer approves the conference registration

Change Swagger **Authorize** back to:

```text
demo-organizer
```

Execute `GET /api/events/{conferenceEventId}/registrations`.

Confirm the attendee appears in the organizer guest list as Pending.

Execute:

```text
POST /api/registrations/{conferenceRegistrationId}/approve
```

Expected:

- `200 OK`
- Status changes to `Confirmed`.
- A new `confirmationReference` starts with `EF-`.
- The API console logs a confirmation notification.

Change Swagger back to `demo-attendee` and execute `GET /api/registrations`.
Confirm that the conference registration is now Confirmed and includes the
confirmation reference.

## 7. Attendee cancels the conference registration

As `demo-attendee`, execute:

```text
POST /api/registrations/{conferenceRegistrationId}/cancel
```

Expected:

- `200 OK`
- Status changes to `Cancelled`.
- The attendee no longer sees it in `GET /api/registrations`.
- The API console logs a registration-cancellation notification.

Change to `demo-organizer` and execute the conference guest-list endpoint.
The cancelled row must remain visible as historical data.

Try to register again as `demo-attendee`. Expected:

```text
409 Conflict
```

Version 1 permits only one registration attempt per user and event.

## 8. Rejection scenario with a second conference

As `demo-organizer`, create a second event with a different seed:

```json
{
  "title": "Sustainable Cities Leadership Forum",
  "eventType": "Forum",
  "description": "A leadership forum about sustainable city planning.",
  "venueId": "44444444-4444-4444-4444-444444444444",
  "startUtc": "2027-06-12T09:00:00Z",
  "endUtc": "2027-06-12T16:00:00Z",
  "registrationDeadlineUtc": "2027-06-11T23:59:00Z",
  "capacity": 150,
  "visibility": "Unlisted",
  "approvalRequired": true
}
```

Save its `eventId` as `forumEventId`.

As `demo-attendee`, use the direct event endpoint with `forumEventId`, then
register. Save the returned registration as `forumRegistrationId`.

As `demo-organizer`, execute:

```text
POST /api/registrations/{forumRegistrationId}/reject
```

Body:

```json
{
  "reason": "The forum requires a different participant profile."
}
```

Expected:

- `200 OK`
- Status is `Cancelled`.
- The reason is stored.
- No confirmation reference exists.
- The guest-list row remains preserved.

## 9. Postponement and rescheduling scenario

As `demo-organizer`, create a separate Active event:

```json
{
  "title": "Cloud Operations Roundtable",
  "eventType": "Roundtable",
  "description": "A technical discussion for cloud operations teams.",
  "venueId": "33333333-3333-3333-3333-333333333333",
  "startUtc": "2027-07-08T13:00:00Z",
  "endUtc": "2027-07-08T16:00:00Z",
  "registrationDeadlineUtc": "2027-07-07T23:59:00Z",
  "capacity": 35,
  "visibility": "Public",
  "approvalRequired": false
}
```

Save the returned `eventId` as `roundtableEventId`.

Execute:

```text
POST /api/events/{roundtableEventId}/postpone
```

Body:

```json
{
  "reason": "The technical venue needs urgent maintenance."
}
```

Expected:

- Status changes to `Postponed`.
- A new attendee registration receives `409 Conflict`.
- An event-postponement notification is logged for active registrations.

Execute:

```text
POST /api/events/{roundtableEventId}/reschedule
```

Body:

```json
{
  "startUtc": "2027-08-08T13:00:00Z",
  "endUtc": "2027-08-08T16:00:00Z",
  "registrationDeadlineUtc": "2027-08-07T23:59:00Z",
  "venueId": "33333333-3333-3333-3333-333333333333"
}
```

Expected:

- Status returns to `Active`.
- The new schedule is returned.
- A reschedule notification is logged.

## 10. Closed event scenario

Create a separate event using the large venue:

```json
{
  "title": "Executive Data Governance Briefing",
  "eventType": "Briefing",
  "description": "A briefing on organizational data governance.",
  "venueId": "44444444-4444-4444-4444-444444444444",
  "startUtc": "2027-09-10T14:00:00Z",
  "endUtc": "2027-09-10T16:00:00Z",
  "registrationDeadlineUtc": "2027-09-09T23:59:00Z",
  "capacity": 80,
  "visibility": "Public",
  "approvalRequired": false
}
```

Execute:

```text
POST /api/events/{briefingEventId}/close
```

Expected:

- Status becomes `Closed`.
- New registrations return `409 Conflict`.
- Rescheduling returns `409 Conflict`.
- The event remains stored and cannot be reopened.

## 11. Cancelled event scenario

Create a final event:

```json
{
  "title": "Product Launch Demonstration",
  "eventType": "Demonstration",
  "description": "A public demonstration of a new product.",
  "venueId": "44444444-4444-4444-4444-444444444444",
  "startUtc": "2027-10-18T10:00:00Z",
  "endUtc": "2027-10-18T12:00:00Z",
  "registrationDeadlineUtc": "2027-10-17T23:59:00Z",
  "capacity": 200,
  "visibility": "Public",
  "approvalRequired": false
}
```

Execute:

```text
POST /api/events/{launchEventId}/cancel
```

Body:

```json
{
  "reason": "The product launch has been cancelled."
}
```

Expected:

- Status becomes `Cancelled`.
- New registrations return `409 Conflict`.
- Repeating cancellation returns `409 Conflict`.
- Active registrants, if any, receive logged cancellation notifications.

## 12. Negative checks for this seed

| Test | Action | Expected |
|---|---|---|
| Attendee creates forum | Authorize `demo-attendee`, `POST /api/events` | `403 Forbidden` |
| Missing identity | Clear Swagger authorization, call `/api/identity/me` | `401 Unauthorized` |
| Duplicate conference registration | Register again after cancellation | `409 Conflict` |
| Capacity over large venue | Create event with capacity `501` | `409 Conflict` |
| Capacity over small venue | Create event with capacity `51` at venue `333...` | `409 Conflict` |
| Invalid dates | End before start or deadline after start | `400 Bad Request` |
| Unknown venue | Use a random venue ID | `404 Not Found` |
| Reschedule Active event | Call reschedule without postponing | `409 Conflict` |
| Approve rejected registration | Approve the rejected forum registration | `409 Conflict` |
| Wrong ownership | Use a non-owner organizer if available | `403 Forbidden` |

## 13. Pass criteria

This alternate seed passes when:

- The large venue accepts capacities through `500` but rejects `501`.
- The small venue accepts capacities through `50` but rejects `51`.
- Public conference search returns the Technology conference.
- Unlisted forum access works by direct event identifier.
- Approval produces Confirmed plus an `EF-...` reference.
- Pending registration produces no confirmation.
- Attendee cancellation and organizer rejection preserve history.
- Postponement blocks registration and rescheduling restores Active status.
- Closed and Cancelled events cannot accept registrations.
- Swagger identity switching correctly enforces organizer and attendee roles.
- All negative checks return the documented status codes.
