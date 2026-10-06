# Upgrade from the previous SQLite package

The previous package was an ASP.NET Core application with a JavaScript frontend, not WebAssembly. This replacement is Blazor Server (Interactive Server) with EF Core and SQL Server.

## If you only viewed the previous package

Open this replacement solution and run it. Startup applies EF migrations to Chwaya_rentalDb. Create an administrator and add your rental records.

## If you entered records in the previous application

EF schema migrations do not copy SQLite data automatically. The optional Python exporter converts the old database into a SQL Server import script and preserves record IDs and contract snapshots.

1. Stop the old application and back up its whole Data directory.
2. Run the new application once to create Chwaya_rentalDb and apply EF migrations. Do not create its administrator or enter new records yet.
3. Stop the new application.
4. From this project folder, run:

       python Tools/import_legacy_sqlite.py C:\OldRentalManager\Data\rental.db C:\Private\legacy-import.sql

5. Review the generated SQL. Connect in SSMS to the chosen SQL Server instance and the empty Chwaya_rentalDb.
6. Execute the generated SQL. It refuses to import if any target rental table already contains data. All inserts run in one transaction.
7. Restart the new application and verify tenant/account details, property counts, payments and contract snapshots.

Existing administrator password hashes are preserved, so use your old credentials if the old database had an administrator. The export file contains confidential information, including password hashes, account numbers and national IDs; protect it and remove it after verified import.

The importer supports the database produced by the previous package in this conversation. It does not read any payroll database. Every imported old house is typed House; add new shops from the Shops screen. SQL Server constraints reject inconsistent or overlapping active leases rather than silently changing them.
