# Phase 5: event services and endpoints

Phase 5 adds public event discovery and organizer-owned event management.

## Public endpoints

```text
GET /api/events
GET /api/events?title=workshop
GET /api/events/{eventId}
```

Search returns only future, Active, Public events whose registration deadline
has not passed. Title matching is partial. An Unlisted event can be read when
its direct identifier is known.

## Organizer endpoints

Organizer operations require:

```http
X-Demo-Identity: demo-organizer
```

```text
POST /api/events
GET /api/events/owned/{eventId}
PUT /api/events/{eventId}
```

The owner comes from the resolved identity. Ordinary updates change only title,
event type, and description. Schedule, venue, capacity, visibility, approval,
and lifecycle changes belong to dedicated later workflows.

## Service rules

- The selected venue must exist.
- Event capacity cannot exceed venue capacity.
- Active events cannot overlap at the same venue.
- New events start in the Active state.
- Only Active events accept ordinary updates.
- Capacity and schedule conflicts return `409 Conflict`.
- Missing resources return `404 Not Found`.

## Verification

```powershell
dotnet build .\EventFlow.sln
dotnet run --project ".\src\EventFlow.Api\EventFlow.Api.csproj"
```

Use Swagger or PowerShell with `X-Demo-Identity` to test public search,
unlisted reads, organizer creation, ownership boundaries, venue-capacity
conflicts, and overlapping schedule conflicts.
