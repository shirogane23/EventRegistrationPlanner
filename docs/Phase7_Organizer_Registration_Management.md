# Phase 7: Organizer Registration Management

Phase 7 adds the organizer workflow for reviewing and managing registrations for
events owned by the current demo organizer.

## Endpoints

All organizer operations require `X-Demo-Identity: demo-organizer`.

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/events/{eventId}/registrations` | Read the registration list for an owned event |
| `POST` | `/api/registrations/{registrationId}/approve` | Approve a Pending registration |
| `POST` | `/api/registrations/{registrationId}/reject` | Reject a Pending registration |
| `POST` | `/api/registrations/{registrationId}/organizer-cancel` | Cancel a Pending or Confirmed registration |

Rejection and organizer cancellation require a body such as:

```json
{
  "reason": "The attendee request cannot be accepted for this event."
}
```

Approval does not require a request body.

## Rules implemented

- Only the event owner can read or change its registrations.
- Only Pending registrations can be approved or rejected.
- Approval is allowed after the registration deadline, but not after the event
  starts or leaves Active status.
- Approval changes the registration to `Confirmed` and creates a confirmation
  reference.
- Rejection changes the registration to `Cancelled`, stores the required reason,
  and leaves the confirmation reference empty.
- Organizer cancellation changes a Pending or Confirmed registration to
  `Cancelled` and stores the required reason.
- Cancelled rows are retained as history and no longer count toward capacity.
- Invalid transitions return `409 Conflict`; missing resources return `404 Not
  Found`; missing or invalid identity/ownership returns `401 Unauthorized` or
  `403 Forbidden`.

The guest-list response includes attendee identity, status, confirmation
reference, decision reason, and timestamps. It includes cancelled rows so the
organizer can distinguish historical decisions from active capacity.

## Verification examples

```powershell
$organizer = @{ "X-Demo-Identity" = "demo-organizer" }

# Read the guest list for an owned event.
Invoke-RestMethod `
  -Uri "http://localhost:5200/api/events/{eventId}/registrations" `
  -Headers $organizer

# Approve a Pending registration.
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5200/api/registrations/{registrationId}/approve" `
  -Headers $organizer

# Reject a Pending registration.
$body = @{ reason = "The request was not approved for this event." } |
  ConvertTo-Json
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5200/api/registrations/{registrationId}/reject" `
  -Headers $organizer `
  -ContentType "application/json" `
  -Body $body
```

Use `demo-attendee` for the attendee endpoints. A different organizer must
receive `403 Forbidden` when attempting to manage an event or registration it
does not own.
