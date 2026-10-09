# Profile, comments and notifications

Rental module links are grouped in a responsive top navigation bar. Notifications (bell), Settings (gear), the initials profile card and sign-out remain in a persistent bottom account bar. On narrower screens the controls wrap into additional rows. Content uses the full page width and leaves space for the bottom bar.

## User profile

`/profile` saves the administrator's display name, email and phone. The sidebar updates after saving; accounts without a display name use their username. An initials avatar is generated from the name. Profile information never exposes password hashes or security stamps. Login credential changes remain on the protected account pages. This release retains one administrator account.

## Comments

`/comments` contains internal rental notes: subject, plain-text message, author, UTC creation time and open/resolved status. Notes can be searched and filtered, resolved or reopened. Authors come from the authenticated server session. Comments are escaped by Razor and are not sent to tenants. No email/SMS notifications or external recipients are involved. Comments cannot be silently deleted. Results use stable SQL paging (25 per page).

## Notifications

`/notifications` is a persisted in-app inbox for new rental changes, successful account activity, profile updates and new comments. It supports all/unread filters, individual mark-read, mark-all-read, paging and links to the relevant module. The sidebar count updates on navigation and workspace changes. Use Refresh for activity from another browser; this release does not promise push delivery or continuous polling. Historical activity is not backfilled. Mark-all-read affects all currently unread notifications, including other pages.

## Database upgrade

Migration `20261009074500_ProfileCommentsNotifications` adds profile columns to Admins and creates Comments/Notifications with indexes. Existing accounts, rentals, payments, audit history and contracts are preserved. Auto-migration applies it at startup when enabled. Otherwise run:

```bash
dotnet ef database update --project src/RentalManager.Infrastructure --startup-project .
```

Deploy the matching code and migration together. Existing records receive empty profile defaults. Save your name in My profile after signing in.

## Validation

Unit tests cover authorization and invalid inputs before persistence. SQL workflow checks profile persistence, comment author/status/search/paging, notification creation/read state/paging and account preservation when upgrading from the previous migration. GitHub Actions builds the Razor pages and runs a headless browser check for profile persistence, escaped comment rendering, status updates, notification read state, desktop top navigation placement and mobile horizontal overflow. Screenshot artifacts are kept on the workflow run. PDF checks should also be performed in the deployed environment.
