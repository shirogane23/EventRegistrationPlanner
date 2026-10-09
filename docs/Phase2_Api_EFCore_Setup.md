# Phase 2: ASP.NET Core and EF Core setup

Phase 2 creates the `net8.0` ASP.NET Core Web API project, installs the EF
Core SQL Server tooling, reverse-engineers the approved `EventFlowDb` schema,
and registers the generated context through external configuration.

## Project layout

```text
src/
└── EventFlow.Api/
    ├── Controllers/
    ├── Data/
    │   └── EventFlowDbContext.cs
    ├── Models/
    │   ├── Event.cs
    │   ├── Registration.cs
    │   ├── User.cs
    │   └── Venue.cs
    ├── Program.cs
    ├── appsettings.json
    └── EventFlow.Api.csproj
```

The files under `Data/` and `Models/` are generated from SQL Server. Do not
hand-edit them. Change the database first and re-run the scaffold command when
the schema changes.

## Prerequisites

- .NET 8 SDK or a compatible newer SDK.
- SQL Server LocalDB instance `(localdb)\MSSQLLocalDB`.
- The Phase 1 `EventFlowDb` database.

## Restore and build

From the repository root:

```powershell
dotnet tool restore
dotnet restore
dotnet build .\EventFlow.sln
```

The repository-local `dotnet-ef` tool is pinned to the same EF Core 8.0.31
version used by the project packages.

## Re-scaffold the database

Run this after an approved database schema change:

```powershell
dotnet tool run dotnet-ef dbcontext scaffold `
  "Server=(localdb)\MSSQLLocalDB;Database=EventFlowDb;Trusted_Connection=True;TrustServerCertificate=True;" `
  Microsoft.EntityFrameworkCore.SqlServer `
  --project ".\src\EventFlow.Api\EventFlow.Api.csproj" `
  --startup-project ".\src\EventFlow.Api\EventFlow.Api.csproj" `
  --context EventFlowDbContext `
  --context-dir Data `
  --output-dir Models `
  --no-onconfiguring `
  --no-pluralize `
  --force
```

`--no-onconfiguring` is required so the connection string remains in
configuration rather than being embedded in generated code.

## Configuration

`src/EventFlow.Api/appsettings.json` contains the development LocalDB
connection string. Use user secrets or environment-specific configuration for
non-local credentials. Do not commit passwords or production connection
strings.

The API registers `EventFlowDbContext` in `Program.cs` using the
`ConnectionStrings:EventFlowDb` configuration key.

## Verification

Run:

```powershell
dotnet build .\EventFlow.sln
dotnet test .\EventFlow.sln
```

There are no automated tests yet; the test command should report that no test
projects were found until a later phase adds them.

To start the API:

```powershell
dotnet run --project ".\src\EventFlow.Api\EventFlow.Api.csproj"
```

Then open the Swagger URL printed by the application. The current project has
an initial `GET /api/health` operation for verifying API and database
connectivity. Domain endpoints are added in later phases.

The health endpoint returns `200 OK` when the API can connect to `EventFlowDb`
and `503 Service Unavailable` when the database cannot be reached.

## Phase 2 completion criteria

- The solution builds successfully.
- EF Core SQL Server, Design, and Tools packages are restored.
- The repository-local `dotnet-ef` tool reports version 8.0.31.
- `EventFlowDbContext` and all four generated entities exist.
- Generated mappings include the approved relationships, indexes, and filtered
  confirmation-reference index.
- No generated context contains an embedded connection string.
- `EventFlowDbContext` is registered through external configuration.
