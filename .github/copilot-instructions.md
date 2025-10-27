## Quick orientation for AI coding agents

This repository is a small ASP.NET Core Web API solution. The goal of these instructions is to help AI coding agents be immediately productive by describing the project's structure, conventions, and common developer workflows.

### Big picture
- Single ASP.NET Core Web API project: `PagueVeloz.API/` (see `PagueVeloz.sln` at repo root).
- Uses the minimal hosting model (see `PagueVeloz.API/Program.cs`). The app registers controllers and OpenAPI via `builder.Services.AddControllers()` and `builder.Services.AddOpenApi()`.
- Target framework: .NET 9.0 (`PagueVeloz.API/PagueVeloz.API.csproj`).

### Key files to inspect
- `PagueVeloz.API/Program.cs` — application startup, service registrations, middleware pipeline. When adding services, register them on `builder.Services` here.
- `PagueVeloz.API/Controllers/WeatherForecastController.cs` — canonical example of a controller and DI usage (ILogger). Use it as a pattern for new controllers.
- `PagueVeloz.API/WeatherForecast.cs` — model/DTO used by the sample controller.
- `PagueVeloz.API/appsettings.json` — configuration defaults (logging, AllowedHosts). Environment-specific config is loaded automatically by ASP.NET Core (e.g., `appsettings.Development.json` in build output).

### Architecture & patterns to follow
- Controllers live in `Controllers/` and use attribute routing (e.g., `[Route("[controller]")]`). Follow the same naming and DI pattern (constructor injection for `ILogger<T>` and services).
- Register new application services in `Program.cs` before `builder.Build()`; resolve them via constructor injection in controllers.
- OpenAPI (Swagger) is enabled via `AddOpenApi()` and exposed in Development with `app.MapOpenApi()`. Avoid changing this exposure without an explicit need.
- Authorization middleware is present (`app.UseAuthorization()`), but there is no built-in auth provider configured in the repo. If you add policies, wire them in `Program.cs` and update `appsettings` accordingly.

### Build, run and debug (developer workflows)
- Build the solution from the repo root:

```powershell
dotnet build PagueVeloz.sln
```

- Run the API locally (from repo root or `PagueVeloz.API`):

```powershell
dotnet run --project PagueVeloz.API\PagueVeloz.API.csproj
```

- The project uses the default ASP.NET Core launch settings; when `ASPNETCORE_ENVIRONMENT` is `Development`, Swagger/OpenAPI UI is mapped (`/swagger` or default endpoints created by `AddOpenApi()`).

### Tests and CI
- No test project was found. If you add tests, place them under a `tests/` folder and include them in the solution (`PagueVeloz.sln`). Use `dotnet test` to run them.

### Dependencies & integration
- Project uses `Microsoft.AspNetCore.OpenApi` (v9.x). There are no other external SDKs or services declared in the project file.
- Configuration and secrets follow standard ASP.NET Core patterns (`appsettings.json`, `appsettings.{Environment}.json`, environment variables). Don't assume custom secret stores unless added explicitly.

### Conventions & petty differences from common templates
- Minimal hosting model only — no `Startup` class. Place DI registrations and middleware directly in `Program.cs`.
- The repository currently contains a single example controller (`WeatherForecastController`); prefer following its simple patterns when adding endpoints (attribute routing, small DTOs, constructor DI).
- Logging is configured via `appsettings.json` default levels. Use `ILogger<T>` throughout.

### Helpful examples for edits
- To add a transient service:

```csharp
// in Program.cs
builder.Services.AddTransient<IMyService, MyService>();
```

```csharp
// in a controller
public class MyController : ControllerBase
{
  public MyController(IMyService myService) { ... }
}
```

### When to ask the developer
- If a change requires: environment-specific secrets, external service credentials, or non-trivial deployment changes (Azure, containers), ask for the target environment and credentials/connection strings.
- If you need to modify auth/authorization behaviour, confirm the intended auth provider and enrollment plan.

### Edit rules for AI agents (practical guardrails)
- Keep edits focused and minimal. Update `Program.cs` only to register required services or middleware for a feature. Avoid large refactors without an explicit request.
- Preserve existing `AddOpenApi()` usage and the `app.MapOpenApi()` behavior unless adding a new API surface or changing environments.
- When adding packages, update the `.csproj` and ensure the project still builds with `dotnet build`.

If anything above is unclear or you want more detail (CI, deployment, or contributor conventions), tell me which area to expand and I'll iterate. 
