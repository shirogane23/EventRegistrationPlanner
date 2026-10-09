# EventFlow database ERD

Phase 1 defines four database entities:

```mermaid
erDiagram
    USER ||--o{ EVENT : owns
    VENUE ||--o{ EVENT : hosts
    USER ||--o{ REGISTRATION : submits
    EVENT ||--o{ REGISTRATION : receives

    USER {
        uniqueidentifier UserId PK
        nvarchar DisplayName
        nvarchar Email UK
        nvarchar DemoIdentity UK
        nvarchar Role
        datetime2 CreatedUtc
    }

    VENUE {
        uniqueidentifier VenueId PK
        nvarchar Name UK
        nvarchar Address
        int Capacity
        datetime2 CreatedUtc
    }

    EVENT {
        uniqueidentifier EventId PK
        uniqueidentifier OwnerUserId FK
        uniqueidentifier VenueId FK
        nvarchar Title
        nvarchar EventType
        nvarchar Description
        datetime2 StartUtc
        datetime2 EndUtc
        datetime2 RegistrationDeadlineUtc
        int Capacity
        nvarchar Visibility
        bit ApprovalRequired
        nvarchar Status
        datetime2 CreatedUtc
        datetime2 UpdatedUtc
    }

    REGISTRATION {
        uniqueidentifier RegistrationId PK
        uniqueidentifier UserId FK
        uniqueidentifier EventId FK
        nvarchar Status
        nvarchar ConfirmationReference UK
        nvarchar DecisionReason
        datetime2 CreatedUtc
        datetime2 UpdatedUtc
    }
```

## Relationship and integrity rules

- One organizer user owns zero or more events.
- One venue hosts zero or more events.
- One user submits zero or more registrations.
- One event receives zero or more registrations.
- `(UserId, EventId)` is unique, so a user has one historical registration row per event.
- `ConfirmationReference` is unique only when populated; multiple Pending or
  Cancelled rows may correctly have no reference.
- Registration rows are never cascaded away by user or event deletion.
- `Pending` and `Confirmed` registrations reserve capacity.
- `Cancelled` registrations do not reserve capacity.
- `Confirmed` registrations require a confirmation reference.
- `Event.Capacity` must be positive and is additionally checked against venue capacity by the application.
- Schedule overlap and atomic capacity reservation are application transaction rules.

## Controlled values

| Column | Allowed values |
|---|---|
| `User.Role` | `Attendee`, `Organizer` |
| `Event.Visibility` | `Public`, `Unlisted` |
| `Event.Status` | `Active`, `Closed`, `Postponed`, `Cancelled` |
| `Registration.Status` | `Pending`, `Confirmed`, `Cancelled` |

All timestamps are stored as UTC `datetime2(0)` values. The SQL script uses
`SYSUTCDATETIME()` for defaults.
