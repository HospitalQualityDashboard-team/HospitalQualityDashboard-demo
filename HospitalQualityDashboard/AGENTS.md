# Repository Guidelines

## Project Structure & Module Organization

This repository is an ASP.NET MVC4 web application targeting .NET Framework 4.7.2. Core server code lives in `Controllers/`, `Models/`, and `Views/`. MVC startup configuration is in `App_Start/`, with application entry points in `Global.asax` and `Global.asax.cs`. Static assets are under `Content/` for CSS and `Scripts/` for JavaScript libraries. Domain reference documents for the hospital quality indicator system are stored in `Tai_Lieu/`. `App_Data/` is reserved for local application data; avoid committing generated or private database files unless explicitly required.

## Build, Test, and Development Commands

Restore NuGet packages before building:

```powershell
nuget restore HospitalQualityDashboard.csproj -PackagesDirectory ..\packages
```

Build the project with MSBuild:

```powershell
msbuild HospitalQualityDashboard.csproj /p:Configuration=Debug
```

For local development, open `HospitalQualityDashboard.csproj` in Visual Studio and run with IIS Express. The project is configured for IIS Express and SSL port `44387`.

## Coding Style & Naming Conventions

Use C# conventions: PascalCase for classes, controllers, action methods, view models, and public properties; camelCase for local variables and parameters. Controller names should end with `Controller`, for example `HomeController`. Razor views should match action names and live under `Views/<ControllerName>/`. Keep indentation at 4 spaces for C# and Razor, and prefer small controllers that delegate reusable business logic to models or service classes when added.

## Testing Guidelines

No test project is currently present. When adding tests, create a separate test project such as `HospitalQualityDashboard.Tests` and use clear names like `ControllerName_ActionName_ExpectedBehavior`. Prioritize tests for permission checks, report status transitions, indicator calculations, and import validation. Run all tests before opening a pull request.

## Commit & Pull Request Guidelines

This directory does not currently expose git history, so no existing commit convention can be inferred. Use concise imperative commit messages, for example `Add indicator assignment model` or `Fix report status validation`. Pull requests should include a short summary, affected modules, manual test steps, screenshots for UI changes, and links to related issues or requirements when available.

## Security & Configuration Tips

Do not commit real credentials, connection strings, uploaded evidence files, or hospital-sensitive data. Keep environment-specific settings in transform files such as `Web.Debug.config` and `Web.Release.config`. Validate all uploaded/imported files and enforce Admin/User access rules on the server side, not only in Razor views.
