# Client portal inside RentalManager

## Administrator workflow

1. Add the tenant and their house/shop lease using the existing modules. Complete Landlord & bank and generate the contract from Leases.
2. Open **Clients** (also linked from Tenants and Settings). Select an existing tenant and create a unique username and a 12–200 character temporary password. One account is allowed per tenant.
3. Give the client `/Client/Login`, their username and temporary password through your normal direct communication. The application does not send passwords by email or SMS. The client must change the temporary password before viewing rental records.
4. Under **Clients → Renewal requests**, view the client's intention to continue or leave. Send a response and mark it reviewed. The response becomes visible in their portal notifications.
5. Agree renewal terms separately, then update/create the lease and generate the correct contract. A client request or review does not automatically change dates, rent or contract snapshots.

Admins can disable/re-enable accounts and reset temporary passwords. These actions rotate security stamps and invalidate previous sessions. Reset passwords must again be replaced before portal access. Passwords are hashed; lists and activity reports do not return hashes or passwords.

## Client workflow

- `/Client/Login`: sign in; change a temporary password when required.
- `/client`: view only your rental terms and monthly rent. Select an active lease to share whether you wish to continue (1–12 requested months) or leave, with an optional message.
- `/client/contracts`: preview/print your own saved contract snapshots, using the existing contract HTML/PDF workflow.
- `/client/notifications`: view rental-expiry reminders and landlord responses. Reminders start within the lease's configured NoticeDays (default 30) and display expired active terms until the landlord closes them. Dates use East Africa time (UTC+3).

Reminders are calculated when the client opens or refreshes the portal. This release does not provide scheduled email/SMS, push delivery or notifications while the client is offline. Client intentions are internal messages to the administrator, not automatic legal notices or approved renewals. One decision per lease is maintained; resubmitting updates it and returns it to pending review. Decisions on leases more than 30 days past their end date require direct landlord contact.

## Access separation

Admin and Client roles use the existing encrypted cookie with role-aware validation. Default authorization requires Admin, so client accounts cannot access administrator modules or document endpoints. The Client policy also requires completion of the temporary-password change. Existing validated administrator cookies gain the Admin role on their next request. Client identity includes account and tenant IDs; database validation checks them against the enabled account and its security stamp.

Client contract queries join through the authenticated tenant's leases; another tenant's contract returns not found. Client renewal writes check the lease's tenant and active status inside a transaction. Leases with payments, contracts or renewal requests cannot be reassigned to another tenant. Client accounts protect linked tenants against deletion. Existing long-running circuits are revalidated every five minutes; cookie requests are validated on each request.

The current login session is shared within a browser profile. Signing in as a client replaces the administrator session in that browser profile; use separate browser profiles for simultaneous admin/client testing.

## Database upgrade

Apply additive migration `20261009155500_ClientPortal` with the matching application code. It creates ClientAccounts and RenewalIntents, including tenant/lease foreign keys and unique indexes. Existing rentals, accounts, contracts and reports remain intact.

```bash
dotnet ef database update --project src/RentalManager.Infrastructure --startup-project .
```

Startup applies it automatically when Database:AutoMigrate is enabled. Provision client accounts after the upgrade; there is no public self-registration.

## Validation

Unit tests cover unauthenticated client boundaries, rejection at administrator boundaries and notice-period/East Africa date calculations. SQL workflow tests cover first-use password changes, stamp invalidation, account disabling/resets, tenant-owned contract/renewal access, request responses and prevention of lease reassignment. Browser checks exercise separate administrator/client sessions and the client UI.
