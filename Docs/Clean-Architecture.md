# RentalManager Clean Architecture

## Projects and dependency direction

- **Domain** (`src/RentalManager.Domain`): entities, field constraints, no persistence or presentation dependencies.
- **Application** (`src/RentalManager.Application`): authenticated use-case facade, repository/current-user/account/renderer contracts, report data, rent allocation, receipt grouping and CSV calculations. References Domain only.
- **Infrastructure** (`src/RentalManager.Infrastructure`): EF Core SQL Server repository, schema/migrations, account password hashing. Implements Application interfaces; references Application and Domain.
- **Web** (root `RentalManager.csproj`): existing Blazor Server and Razor account pages, cookie sessions, current-user adapter, HTML contract/PDF presentation and dependency composition. Existing startup and deployment paths remain valid.
- **Tests**: workflow, report, allocation, grouping and application authorization boundary tests.

Namespaces and table/entity names are preserved to avoid unintended schema recreation. The SQL Server connection stays exclusively on the server. Existing migration classes were moved without changing migration IDs. An additive EnforceLeaseOverlap migration installs the trigger missing from the original EF migration path; run database update before deploying this release. Existing data and contract snapshots are not replaced. New migrations belong to Infrastructure. The design-time configuration factory remains in Web, where configuration and user secrets are available.

The persistence implementation retains transaction-sensitive lease validation, overlap protection and audit writes. These rules remain next to their SQL transaction to preserve concurrency correctness. Pure allocation/reporting rules are in Application. Web account pages no longer query EF directly.

## Verified source improvements

1. Removed `.Result` and unnecessary asynchronous cell-formatting methods from rendering.
2. Compute annual totals from one annual calculation per report render, rather than six separate annual calculations.
3. Calculate monthly rent paid with grouped lease lookups instead of scanning every payment for every lease; tenant/property lookup dictionaries replace repeated searches.
4. Receipt filters use a tenant lease ID set instead of a payment-by-lease nested scan.
5. Audit pages skip loading properties, tenants, leases and payments. Core screens skip contract/audit queries. Database reads retain `AsNoTracking`.
6. Single-month rent saves now enforce lease-month bounds and two-decimal payment precision, matching instalment validation.
7. Translate persistence errors at the Infrastructure boundary, keeping EF dependencies out of Application.
8. Source checker now matches the real project and migration layout; GitHub Actions builds and runs unit tests plus an isolated SQL Server workflow test.

These changes reduce repeated work; no measured latency or throughput improvement is claimed. Core screens still load all core records. Very large portfolios need database-side pagination and filtered report queries as a future change, rather than a silent truncation that would corrupt totals.

## Validation

```bash
python3 Tools/source_check.py
dotnet restore RentalManager.sln
dotnet build RentalManager.sln --configuration Release
dotnet test Tests/RentalManager.Tests.csproj --configuration Release --filter 'FullyQualifiedName!~HousesAndShopsUseTheSamePaymentsAndContracts'
```

Run the SQL integration test separately against a test-only SQL Server using `RENTAL_TEST_CONNECTION`; it creates and removes an isolated database and verifies migrations and transactional workflows. Run Visual Studio startup, sign-in/setup/password change, house/shop CRUD, grouped payments, reports, and contract/PDF generation before deployment.

## Execution limits

GitHub Actions successfully restored and compiled the solution, passed all 11 non-SQL tests, and passed the SQL Server migration/workflow test. The SQL workflow check identified a missing database overlap trigger; an additive migration now installs it for new and existing databases. The passing workflow covers house/shop payments, overlap rejection through both the service and direct SQL persistence, immutable escaped contracts, and linked-record deletion protection. The editing environment has no .NET SDK or SQL Server. Source/XML, JavaScript syntax and whitespace checks can run here; compilation, package restore, migrations, rendering and SQL integration tests cannot run locally. CI is provided to perform the .NET checks. Do not treat this refactor as evidence that every runtime issue has been found or fixed. The existing EF package version is retained rather than changing it during the architecture refactor.
