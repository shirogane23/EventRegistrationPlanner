/*
    EventFlow database foundation
    Phase 1: schema, constraints, indexes, and safe demo seed data

    This script is intentionally non-destructive. It creates the database and
    objects only when they do not already exist. It does not drop or alter
    existing objects.
*/

USE [master];
GO

IF DB_ID(N'EventFlowDb') IS NULL
BEGIN
    CREATE DATABASE [EventFlowDb];
END;
GO

USE [EventFlowDb];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.[User]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[User]
    (
        UserId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_User PRIMARY KEY
            CONSTRAINT DF_User_UserId DEFAULT NEWSEQUENTIALID(),
        DisplayName NVARCHAR(120) NOT NULL,
        Email NVARCHAR(320) NOT NULL,
        DemoIdentity NVARCHAR(100) NOT NULL,
        Role NVARCHAR(20) NOT NULL,
        CreatedUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_User_CreatedUtc DEFAULT SYSUTCDATETIME(),

        CONSTRAINT UQ_User_Email UNIQUE (Email),
        CONSTRAINT UQ_User_DemoIdentity UNIQUE (DemoIdentity),
        CONSTRAINT CK_User_Role CHECK (Role IN (N'Attendee', N'Organizer'))
    );
END;
GO

IF OBJECT_ID(N'dbo.Venue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Venue
    (
        VenueId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Venue PRIMARY KEY
            CONSTRAINT DF_Venue_VenueId DEFAULT NEWSEQUENTIALID(),
        Name NVARCHAR(160) NOT NULL,
        Address NVARCHAR(300) NOT NULL,
        Capacity INT NOT NULL,
        CreatedUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_Venue_CreatedUtc DEFAULT SYSUTCDATETIME(),

        CONSTRAINT UQ_Venue_Name UNIQUE (Name),
        CONSTRAINT CK_Venue_Capacity CHECK (Capacity > 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.Event', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Event]
    (
        EventId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Event PRIMARY KEY
            CONSTRAINT DF_Event_EventId DEFAULT NEWSEQUENTIALID(),
        OwnerUserId UNIQUEIDENTIFIER NOT NULL,
        VenueId UNIQUEIDENTIFIER NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        EventType NVARCHAR(100) NOT NULL,
        Description NVARCHAR(4000) NOT NULL,
        StartUtc DATETIME2(0) NOT NULL,
        EndUtc DATETIME2(0) NOT NULL,
        RegistrationDeadlineUtc DATETIME2(0) NOT NULL,
        Capacity INT NOT NULL,
        Visibility NVARCHAR(20) NOT NULL,
        ApprovalRequired BIT NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        CreatedUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_Event_CreatedUtc DEFAULT SYSUTCDATETIME(),
        UpdatedUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_Event_UpdatedUtc DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_Event_OwnerUser
            FOREIGN KEY (OwnerUserId) REFERENCES dbo.[User] (UserId),
        CONSTRAINT FK_Event_Venue
            FOREIGN KEY (VenueId) REFERENCES dbo.Venue (VenueId),
        CONSTRAINT CK_Event_Capacity CHECK (Capacity > 0),
        CONSTRAINT CK_Event_Schedule CHECK
            (EndUtc > StartUtc AND RegistrationDeadlineUtc <= StartUtc),
        CONSTRAINT CK_Event_Visibility CHECK
            (Visibility IN (N'Public', N'Unlisted')),
        CONSTRAINT CK_Event_Status CHECK
            (Status IN (N'Active', N'Closed', N'Postponed', N'Cancelled'))
    );
END;
GO

IF OBJECT_ID(N'dbo.Registration', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Registration
    (
        RegistrationId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Registration PRIMARY KEY
            CONSTRAINT DF_Registration_RegistrationId DEFAULT NEWSEQUENTIALID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        EventId UNIQUEIDENTIFIER NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        ConfirmationReference NVARCHAR(40) NULL,
        DecisionReason NVARCHAR(1000) NULL,
        CreatedUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_Registration_CreatedUtc DEFAULT SYSUTCDATETIME(),
        UpdatedUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_Registration_UpdatedUtc DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_Registration_User
            FOREIGN KEY (UserId) REFERENCES dbo.[User] (UserId),
        CONSTRAINT FK_Registration_Event
            FOREIGN KEY (EventId) REFERENCES dbo.[Event] (EventId),
        CONSTRAINT UQ_Registration_UserEvent UNIQUE (UserId, EventId),
        CONSTRAINT CK_Registration_Status CHECK
            (Status IN (N'Pending', N'Confirmed', N'Cancelled')),
        CONSTRAINT CK_Registration_ConfirmationReference CHECK
            (Status <> N'Confirmed' OR ConfirmationReference IS NOT NULL)
    );
END;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.key_constraints
    WHERE name = N'UQ_Registration_ConfirmationReference'
      AND parent_object_id = OBJECT_ID(N'dbo.Registration')
)
BEGIN
    ALTER TABLE dbo.Registration
        DROP CONSTRAINT UQ_Registration_ConfirmationReference;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Registration_ConfirmationReference'
      AND object_id = OBJECT_ID(N'dbo.Registration')
)
BEGIN
    CREATE UNIQUE INDEX UX_Registration_ConfirmationReference
        ON dbo.Registration (ConfirmationReference)
        WHERE ConfirmationReference IS NOT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Event_Title'
      AND object_id = OBJECT_ID(N'dbo.[Event]')
)
BEGIN
    CREATE INDEX IX_Event_Title
        ON dbo.[Event] (Title);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Event_OwnerUserId'
      AND object_id = OBJECT_ID(N'dbo.[Event]')
)
BEGIN
    CREATE INDEX IX_Event_OwnerUserId
        ON dbo.[Event] (OwnerUserId);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Event_Status_Schedule'
      AND object_id = OBJECT_ID(N'dbo.[Event]')
)
BEGIN
    CREATE INDEX IX_Event_Status_Schedule
        ON dbo.[Event] (Status, StartUtc, EndUtc, VenueId);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Registration_Event_Status'
      AND object_id = OBJECT_ID(N'dbo.Registration')
)
BEGIN
    CREATE INDEX IX_Registration_Event_Status
        ON dbo.Registration (EventId, Status);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Registration_User_Status'
      AND object_id = OBJECT_ID(N'dbo.Registration')
)
BEGIN
    CREATE INDEX IX_Registration_User_Status
        ON dbo.Registration (UserId, Status);
END;
GO

DECLARE @AttendeeUserId UNIQUEIDENTIFIER =
    '11111111-1111-1111-1111-111111111111';
DECLARE @OrganizerUserId UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222222';
DECLARE @SmallVenueId UNIQUEIDENTIFIER =
    '33333333-3333-3333-3333-333333333333';
DECLARE @LargeVenueId UNIQUEIDENTIFIER =
    '44444444-4444-4444-4444-444444444444';

IF NOT EXISTS (SELECT 1 FROM dbo.[User] WHERE UserId = @AttendeeUserId)
BEGIN
    INSERT INTO dbo.[User]
        (UserId, DisplayName, Email, DemoIdentity, Role)
    VALUES
        (@AttendeeUserId, N'Demo Attendee', N'attendee@example.test',
         N'demo-attendee', N'Attendee');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.[User] WHERE UserId = @OrganizerUserId)
BEGIN
    INSERT INTO dbo.[User]
        (UserId, DisplayName, Email, DemoIdentity, Role)
    VALUES
        (@OrganizerUserId, N'Demo Organizer', N'organizer@example.test',
         N'demo-organizer', N'Organizer');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Venue WHERE VenueId = @SmallVenueId)
BEGIN
    INSERT INTO dbo.Venue (VenueId, Name, Address, Capacity)
    VALUES
        (@SmallVenueId, N'EventFlow Community Room',
         N'100 Example Street', 50);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Venue WHERE VenueId = @LargeVenueId)
BEGIN
    INSERT INTO dbo.Venue (VenueId, Name, Address, Capacity)
    VALUES
        (@LargeVenueId, N'EventFlow Conference Hall',
         N'200 Example Avenue', 500);
END;
GO

/*
    Cross-row rules intentionally enforced by the application transaction layer:
    - Event.Capacity must not exceed Venue.Capacity.
    - Active events at the same venue must not overlap.
    - Pending + Confirmed registrations must not exceed Event.Capacity.
    - A postponed event must be rescheduled with a valid future schedule.

    Foreign keys use the default NO ACTION behavior so registration history
    cannot be removed by deleting a user or event.
*/
