# Phase 1: database setup instructions

## Prerequisites

Install or have access to:

- SQL Server 2019 or later.
- SQL Server Management Studio, Azure Data Studio, or `sqlcmd`.
- Permission to create the `EventFlowDb` database and its tables.

The script uses Windows integrated authentication by default only as an
execution convention; the script itself does not contain credentials.

## 1. Review the schema

Before running the script, review:

- `database/001_CreateEventFlowDatabase.sql`
- `docs/EventFlow_ERD.md`
- The approved planning paper, especially its final approval checklist.

The script creates `User`, `Venue`, `Event`, and `Registration`. It is
non-destructive: it creates missing objects and seed rows, but does not drop or
alter existing objects.

## 2. Execute the setup script

From SQL Server Management Studio:

1. Open `database/001_CreateEventFlowDatabase.sql`.
2. Connect to the approved SQL Server instance.
3. Confirm the target is the intended development SQL Server.
4. Execute the complete script.
5. Confirm that the active database context changes to `EventFlowDb`.

Using `sqlcmd` with the LocalDB instance and Windows authentication:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i ".\database\001_CreateEventFlowDatabase.sql"
```

If the instance or authentication mode differs, replace only the connection
options. Do not add credentials to the repository or to the script. For
example, a full SQL Server instance may use `localhost\SQLEXPRESS`.

## 3. Verify the database objects

Run the following queries against `EventFlowDb`:

```sql
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = N'dbo'
ORDER BY TABLE_NAME;

SELECT name
FROM sys.indexes
WHERE object_id IN
(
    OBJECT_ID(N'dbo.[Event]'),
    OBJECT_ID(N'dbo.Registration')
)
ORDER BY object_id, name;

SELECT DemoIdentity, DisplayName, Role
FROM dbo.[User]
ORDER BY DemoIdentity;

SELECT Name, Capacity
FROM dbo.Venue
ORDER BY Name;
```

Expected tables:

- `Event`
- `Registration`
- `User`
- `Venue`

Expected seeded identities:

- `demo-attendee`
- `demo-organizer`

Expected seeded venues:

- `EventFlow Community Room`
- `EventFlow Conference Hall`

## 4. Validate constraints

The following checks should fail intentionally:

```sql
INSERT INTO dbo.[User]
    (DisplayName, Email, DemoIdentity, Role)
VALUES
    (N'Invalid Role User', N'invalid@example.test',
     N'invalid-role', N'Administrator');
```

```sql
INSERT INTO dbo.Registration
    (UserId, EventId, Status)
VALUES
    ('11111111-1111-1111-1111-111111111111',
     '11111111-1111-1111-1111-111111111111',
     N'Pending');
```

The first statement must fail the role check. The second must fail because the
referenced event does not exist. Roll back or do not retain these test
statements in a shared database.

## 5. Phase 1 completion criteria

Phase 1 is complete when:

- The ERD has been reviewed and approved.
- The SQL script executes successfully on the target SQL Server.
- All four tables and expected indexes exist.
- Seed users and venues are present.
- Unique, foreign-key, status, role, schedule, and capacity constraints exist.
- Registration history is protected by non-cascading relationships.
- The team accepts that overlap and atomic capacity rules will be implemented
  in application transactions during later phases.

Do not run EF Core scaffolding until these criteria are accepted. The generated
entity names and relationship shape in later phases depend on this schema.
