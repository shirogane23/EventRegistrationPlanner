# Phase 10: Verification and Swagger Authorization

Phase 10 makes the demo identity seam usable directly from Swagger UI and
documents the verification matrix for the completed API.

## Swagger authorization

1. Start the API:

   ```powershell
   dotnet run --project .\src\EventFlow.Api\EventFlow.Api.csproj --urls http://localhost:5200
   ```

2. Open `http://localhost:5200/swagger`.
3. Select **Authorize** near the top of the Swagger UI.
4. Enter `demo-organizer` or `demo-attendee`.
5. Select **Authorize**, then **Close**.
6. Select **Try it out** on an endpoint and execute it.

Swagger now sends the value as the `X-Demo-Identity` request header. This is
Swagger UI configuration only; it is not a frontend login screen and it is not
production authentication.

Use `demo-organizer` for event creation, owned-event operations, lifecycle
transitions, guest lists, and organizer registration actions. Use
`demo-attendee` for registration creation, current registrations, and attendee
cancellation.

## Verification matrix

| Area | Expected verification |
|---|---|
| Health | `GET /api/health` returns `200` and database connectivity |
| Missing identity | Protected endpoint returns `401` |
| Wrong role | Valid identity without required role returns `403` |
| Event ownership | Organizer cannot manage another organizer's event |
| Public search | Only eligible Public Active events are returned |
| Unlisted access | Direct identifier access remains possible |
| Registration duplicate | Second attempt returns `409` |
| Capacity | Pending and Confirmed registrations consume capacity |
| Approval | Pending becomes Confirmed with a reference |
| Rejection | Pending becomes Cancelled with a reason |
| Lifecycle | Active, Closed, Postponed, and Cancelled transitions follow the rules |
| Swagger identity | Authorize adds `X-Demo-Identity` to Try it out requests |

The API must be built before testing:

```powershell
dotnet build .\EventFlow.sln
git diff --check
```

Runtime scenarios requiring SQL Server use the configured
`(localdb)\MSSQLLocalDB` instance and the seeded `EventFlowDb` database.
