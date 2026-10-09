# Phase 4: demo identity and authorization

Phase 4 adds the centralized academic-demo identity seam. It is deliberately
not production authentication. It does not provide password security, token
integrity, account recovery, or real-world identity verification.

## Identity mechanism

Clients send the seeded identity in this header:

```text
X-Demo-Identity: demo-attendee
```

Available seeded identities:

| Header value | Role |
|---|---|
| `demo-attendee` | `Attendee` |
| `demo-organizer` | `Organizer` |

The resolver trims the header value, looks up the matching seeded user with
`AsNoTracking`, and distinguishes missing headers from unknown identities.

## Demonstration endpoint

Use:

```text
GET /api/identity/me
```

With `X-Demo-Identity: demo-attendee`, the endpoint returns the resolved
attendee's identifier, display name, email, and role.

Expected failures:

- Missing header: `401 Unauthorized`.
- Unknown header value: `401 Unauthorized`.

The endpoint intentionally does not return the `DemoIdentity` value itself.

## Authorization services

`EventFlowAuthorizationService` provides reusable checks for later controllers:

- `RequireUserAsync`: verifies the demo identity.
- `RequireRoleAsync`: verifies attendee or organizer role.
- `RequireEventOwnerAsync`: verifies organizer role and event ownership.
- `RequireRegistrationOwnerAsync`: verifies registration ownership.

Controllers can convert failed decisions to consistent `401` or `403`
Problem Details responses with `ToActionResult`.

## Security boundaries

- No endpoint creates organizer accounts or venues.
- Roles come only from seeded database rows.
- Ownership is checked against the database, not request payload fields.
- A valid identity without the required role receives `403 Forbidden`.
- Missing or unknown identities receive `401 Unauthorized`.
- Health checks remain usable without a demo identity.

## Phase 4 completion criteria

- Identity lookup is centralized and database-backed.
- Missing and invalid identities produce `401`.
- Role failures produce `403`.
- Event and registration ownership checks are reusable.
- The identity demonstration endpoint appears in Swagger.
- No password, token, or production-authentication behavior is implied.
