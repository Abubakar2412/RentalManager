# Chwaya Rental Manager — Blazor Server + EF Core + SQL Server

This version replaces the earlier JavaScript/SQLite application with Blazor Server, implemented as a .NET 10 Blazor Web App using Interactive Server rendering only. There is no WebAssembly client project. Razor components run on the server through SignalR. All rental records use normalized SQL Server tables mapped with Entity Framework Core.

Houses and shops share the same leases, deposits, rent due dates, payment methods, rental-period allocation, monthly statements and contract generation.

## Open and run

1. Extract the ZIP and open RentalManager.sln in Visual Studio 2026 with ASP.NET and web development and the .NET 10 SDK.
2. Ensure SQL Server instance LAPTOP-6EC24DAD\CHWAYALAPTOP is running and your Windows user can connect and create databases.
3. Restore NuGet packages, then build.
4. Press F5. Startup creates Chwaya_rentalDb and applies the included EF Core migration.
5. Create your administrator, then sign in.
6. Complete Landlord & settings, including national ID, contacts and receiving account details.
7. Add houses and shops, tenants, leases, then payments.
8. Generate a contract from Leases, then open it in Contracts to print or save PDF.

Neither payroll database is targeted or modified. The old database names have been replaced in both connection templates.

## Connection configuration

Development defaults to:

    Server=LAPTOP-6EC24DAD\CHWAYALAPTOP;Database=Chwaya_rentalDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true

The original connection-string keys are retained. Database:ConnectionName selects exactly one connection; only that selected server is migrated.

- Development: DefaultConnectionOnHrPayMisHubDbDev
- Production: DefaultConnectionOnHrPayMisHubDbLive, selected by appsettings.Production.json.

Live template:

    Server=WIN-JGNQR15UJO3;Database=Chwaya_rentalDb;User ID=sa;Password=REPLACE_VIA_SECRET_CONFIGURATION;MultipleActiveResultSets=true;TrustServerCertificate=true;Max Pool Size=32767;

The actual live password is deliberately omitted from the source ZIP. For development, right-click the project → Manage User Secrets and configure:

    {
      "Database": {
        "ConnectionName": "DefaultConnectionOnHrPayMisHubDbLive"
      },
      "ConnectionStrings": {
        "DefaultConnectionOnHrPayMisHubDbLive": "Server=WIN-JGNQR15UJO3;Database=Chwaya_rentalDb;User ID=sa;Password=YOUR_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=true;Max Pool Size=32767;"
      }
    }

User Secrets load in Development. For Production/IIS, supply the connection through deployment secret configuration, such as the environment key ConnectionStrings__DefaultConnectionOnHrPayMisHubDbLive. Change AllowedHosts to the deployed hostname. Keep passwords out of source control.

The template preserves your requested sa user, trust-certificate and pool settings. For deployment, use a dedicated SQL login and a trusted SQL certificate. Rotate the password disclosed in the conversation before deployment.

## Automatic migrations

Database:AutoMigrate defaults to true. Startup calls Database.MigrateAsync() and applies only pending migration files through __EFMigrationsHistory.

Included:
- Migrations/20261005130000_InitialRentalSqlServer.cs
- Migrations/RentalDbContextModelSnapshot.cs
- Database/InitialSchema.reference.sql, for review only.

Do not execute the reference SQL first; let EF apply its migration. Startup does not call EnsureCreated or delete a database.

Automatic migration means automatically applying existing migration files. New model changes still need a new migration.

Package Manager Console:

    Add-Migration YourChangeName
    Update-Database

CLI:

    dotnet tool install --global dotnet-ef --version 10.0.0
    dotnet ef migrations has-pending-model-changes
    dotnet ef migrations add YourChangeName
    dotnet ef database update

The selected SQL identity needs schema-create/alter permission when startup migrations are enabled. Alternatively disable Database:AutoMigrate and apply migrations separately using a deployment identity.

Check instance name, authentication, SQL service availability and database-create permissions if startup fails. Existing incompatible tables are not overwritten.

## House and shop requirements

| Requirement | House | Shop |
|---|---|---|
| Tenant identity, contacts and accounts | Same tenant record | Same tenant record |
| Monthly rent, deposit, due day | Shared lease | Shared lease |
| Rent / Deposit / Other payments | Shared payment table | Shared payment table |
| Cash / Bank transfer / Mobile money | Same methods | Same methods |
| Rental-period allocation and statements | Same calculation | Same calculation |
| Contracts | Residential wording | Commercial shop wording |
| Property fields | Bedrooms, bathrooms, amenities | Floor area, business activity, bathrooms, amenities |

Shops require a permitted business activity. Newly created shops have zero bedrooms. Both types can have agreed rent different from advertised rent.

Statements charge a full month's rent for each intersecting calendar month through the current date, including first and last months. There is no automatic proration, tax, late fee or interest. Deposits/Other payments are excluded from rent balances. Negative balance represents credit. Customize CsvExport.Statement for other rules.

Contracts save a dated HTML snapshot with account numbers, IDs, contacts, rooms, rent, deposit, terms, dates, witness and signature spaces. Shop agreements state commercial use and business activity. Review the terms before signing. Electronic signing is not included.

## Database

| Table | Purpose |
|---|---|
| Properties | House/Shop type and property fields |
| Tenants | Identity, contacts and tenant accounts |
| Leases | Property/tenant links, dates and rent |
| Payments | Common house/shop payment workflow |
| Landlords | Receiving bank details and general terms |
| Admins | Administrator hash and security stamp |
| Contracts | Immutable agreement snapshots |
| Audit | Change history |
| __EFMigrationsHistory | EF version tracking |

Foreign keys block deletion of referenced records. National IDs are normalized and unique. A SQL overlap trigger and serializable service transactions prevent overlapping Active leases for a property. Dates are inclusive. Rowversion columns prevent stale updates.

The database starts empty. Previous SQLite records are not automatically imported by EF schema migrations. Preserve the old SQLite database if you entered real records; see UPGRADE.md for the optional import script.

## Modify the source

| File | Purpose |
|---|---|
| Program.cs | Server rendering, authentication, SQL selection, migrations |
| Models.cs | EF entities and validation |
| Data/RentalDbContext.cs | DbSets, relationships and constraints |
| Services/RentalService.cs | Shared CRUD/payment/lease rules |
| Components/Pages | Blazor screens |
| Components/Shared/RecordEditor.razor | Reusable C# form editor |
| Pages/Account | Cookie login, setup, logout and password forms |
| Contract.cs | House/shop agreements |
| Services/CsvExport.cs | Statements and exports |
| Migrations | Versioned schema changes |

A DbContext is created per operation using IDbContextFactory, avoiding a context shared across a Blazor circuit. Service methods check authentication. Account forms use antiforgery protection. Password changes invalidate sessions; long-running circuits revalidate every five minutes.

## Tests and verification

This delivery environment has no .NET SDK or SQL Server. Source/configuration consistency and JavaScript syntax checks were run. C# compilation, EF migration execution and live Blazor/SQL Server integration have not been executed here.

Run locally:

    dotnet restore
    dotnet build
    dotnet ef migrations has-pending-model-changes
    dotnet test Tests/RentalManager.Tests.csproj

The SQL integration test needs SQL Server LocalDB, or set RENTAL_TEST_CONNECTION to a test-capable SQL instance. Tests always replace the database name with a unique Chwaya_RentalTest_<guid> database, apply migrations, test house/shop lease/payment/contract parity, then drop only that generated test database. Chwaya_rentalDb and payroll databases are never targeted by the tests.

## IIS

Publish with dotnet publish -c Release -o publish. Install the .NET 10 Hosting Bundle, use an application pool with No Managed Code, configure HTTPS, AllowedHosts and SQL connection secrets. Enable WebSocket support for SignalR. Preserve Data Protection keys. Back up with SQL Server backup tools.

This is a single-administrator workspace. It does not include online bank payments, tenant self-service, uploads or multiple user roles.

## Monthly reports

Open **Reports** beside **Houses** in the sidebar. Select a month and all tenants or one person. Rent reports cover houses and shops and show rent, payments allocated to that rental month, outstanding balances, overpayment credits and due dates per lease. Payments received use the payment date and include rent, deposits and other payments. Cancelled leases are excluded from agreed rent; ended leases remain visible in their historical months. Partial months use the full agreed monthly rent, matching existing statements. Export the filtered rent table with **Export rent CSV**.
