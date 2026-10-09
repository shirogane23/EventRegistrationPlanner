# Phase 8: Event Lifecycle

Phase 8 adds organizer-owned lifecycle transitions:

- `POST /api/events/{eventId}/close`
- `POST /api/events/{eventId}/postpone`
- `POST /api/events/{eventId}/reschedule`
- `POST /api/events/{eventId}/cancel`

All lifecycle endpoints require `X-Demo-Identity: demo-organizer` and the
organizer must own the event.

`Active` events may be closed or postponed. `Closed` is final. A `Postponed`
event may be rescheduled with a future start, end after start, deadline no
later than start, an existing venue, venue capacity compliance, and no overlap
with another Active event. A successful reschedule returns the event to
`Active`. An event may be cancelled unless it is already cancelled; cancelled
events cannot be reactivated.

Lifecycle changes preserve all registrations. New registrations are blocked
while an event is Closed, Postponed, or Cancelled. The postpone and cancel
request reasons are accepted and validated at the API boundary; the current
database schema has no event-level reason column, so the event status and
timestamps remain the persisted lifecycle record.

Example:

```powershell
$organizer = @{ "X-Demo-Identity" = "demo-organizer" }
Invoke-RestMethod -Method Post `
  -Uri "http://localhost:5200/api/events/{eventId}/postpone" `
  -Headers $organizer `
  -ContentType "application/json" `
  -Body (@{ reason = "The venue is temporarily unavailable." } | ConvertTo-Json)
```
