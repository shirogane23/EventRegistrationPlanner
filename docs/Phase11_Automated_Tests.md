# Phase 11: Automated Tests

Phase 11 adds the `EventFlow.Api.Tests` xUnit project to the solution. The
tests use EF Core InMemory for isolated service tests, so they do not require
SQL Server or launching the API executable.

Run the complete suite from the repository root:

```powershell
dotnet test .\EventFlow.sln
```

The current tests cover:

- Event schedule and registration-action validation.
- Pending behavior for approval-required events.
- No confirmation notification for Pending registrations.
- Duplicate registration conflict handling.
- Postponed-event rescheduling back to Active.

Runtime SQL Server and Swagger verification remain documented separately in
`docs/Phase10_Verification_Swagger.md`. Those scenarios should be run against
the configured `(localdb)\MSSQLLocalDB` instance when Application Control
permits the API process or when an administrator-approved development
environment is available.
