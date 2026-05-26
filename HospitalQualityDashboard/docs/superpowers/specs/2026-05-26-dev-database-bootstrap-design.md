# Dev Database Bootstrap Design

## Goal

When a developer clones the project on another machine, updates the `HospitalQualityConnection` connection string, and starts the app with IIS Express in Debug mode, the app should create the SQL Server database and base schema automatically if they do not exist.

The feature is intended for local development only. It must not behave like an always-on production migration system.

## Scope

- Run only when ASP.NET debugging is enabled through `<compilation debug="true" />`.
- Use the existing `HospitalQualityConnection` connection string from `Web.config`.
- Create the target database if it does not already exist.
- Run local SQL scripts from `App_Data/Sql` to create or update the schema.
- Seed the default admin account by running the existing seed script.
- Fail loudly with a useful startup error when SQL Server is unavailable, the connection string is invalid, or the SQL login does not have enough permission.

## Out Of Scope

- No Entity Framework migration integration.
- No production auto-migration.
- No automatic destructive schema changes.
- No automatic import of business data from Excel or Word files.

## Architecture

Add a small startup service, `DatabaseBootstrapper`, under `Services`.

`Global.asax.cs` will call this service near the start of `Application_Start`, before routes and bundles are registered. The service will decide whether it should run by checking `HttpContext.Current.IsDebuggingEnabled`.

The service will:

1. Read `ConfigurationManager.ConnectionStrings["HospitalQualityConnection"]`.
2. Parse the connection string with `SqlConnectionStringBuilder`.
3. Capture the target database name from `Initial Catalog` or `Database`.
4. Build a server-level connection string that points to `master`.
5. Create the target database when `DB_ID(@DatabaseName)` returns null.
6. Connect to the target database.
7. Check whether the base schema exists by testing `OBJECT_ID(N'dbo.KhoaPhong', N'U')`.
8. Run `001_CreateSchema.sql` only when the base schema is missing.
9. Run idempotent follow-up scripts that are safe on existing databases, including admin seeding and additive schema updates.

## SQL Script Handling

Scripts will be read from `App_Data/Sql` using `HostingEnvironment.MapPath`.

The bootstrapper should support SQL files containing `GO` batch separators, because `006_AddNotificationAutomationLog.sql` already contains `GO`. Batch splitting should recognize lines that contain only `GO`, ignoring casing and surrounding whitespace.

Initial script order:

- `001_CreateSchema.sql`: run only when the base schema is missing.
- `002_SeedAdmin.sql`: run every bootstrap attempt because it is idempotent.
- `003_AddIndicatorFrequencies.sql`: run every bootstrap attempt because it guards for existing objects.
- `004_AddAssignmentUniqueConstraint.sql`: run every bootstrap attempt because it guards for existing constraint.
- `005_AddApprovalAndRejection.sql`: run only when `dbo.BaoCao` exists and `YKienPhanHoi` or the updated status constraint is missing; this avoids failing on repeated runs.
- `006_AddNotificationAutomationLog.sql`: run every bootstrap attempt because it guards for existing table.

## Error Handling

Startup should throw an `InvalidOperationException` with context when:

- The connection string is missing.
- The target database name is missing.
- The SQL script directory is missing.
- A required SQL script file is missing.
- SQL execution fails.

The exception message should name the failed script or operation so the developer can fix SQL Server, permissions, or connection string settings quickly.

## Testing

Manual verification will cover:

- Build succeeds.
- Bootstrapper does not run when `debug=false`.
- With a new database name in the connection string, running the app creates the database and base tables.
- Running the app a second time does not fail.
- Existing SQL service code continues to use the same `HospitalQualityConnection`.

Automated tests are not required because the project currently has no test project and this feature depends on local SQL Server state.
