# Settings and administrator setup

Open **Settings** in the main navigation. The module contains:

- **Landlord & bank**: landlord contacts, bank account, currency, standard agreement terms and a PDF report. Saved contract snapshots stay unchanged.
- **Administrator**: current username, password-protected username change, password change and sign-out. First-time setup remains `/Account/Setup`, available only before the single administrator account exists. Username/password changes rotate the security stamp and sign the current browser out. Existing cookies and Blazor sessions are invalidated on their next validation.
- **Activity log**: all stored rental changes plus successful administrator setup/sign-in/sign-out, username and password changes. Filter by administrator/record text, action and inclusive UTC dates. Results are read from SQL in stable pages of 25; CSV and PDF export the displayed page, with no silent 250-record history cutoff. `/audit` remains available for existing bookmarks.

No database migration is required for this module. No passwords, hashes or security stamps are returned in log reports. Account changes and audit writes are saved atomically. Logs have no deletion/edit controls. Existing logs are retained; historical sign-ins cannot be reconstructed.

Validation includes authorization/invalid-filter unit tests and SQL workflow tests for single-admin setup, username/password validation, session invalidation, account audit records, activity paging and date/text filtering. CI also builds the Razor components and pages. A manual browser/PDF check should be performed in the deployed environment.
