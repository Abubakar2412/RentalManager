# Contract-driven rental requirements

Baseline: the supplied Shop No. 6 contract, translated in ShopContract-English.md.

| Requirement | Implementation |
|---|---|
| Parties, addresses including Shehiya/district/region, ID and telephone | Existing landlord and tenant fields; enter the full geographical address |
| Shop unit number and building number | Property name for unit number; new BuildingNumber field |
| Lawful business use | Existing BusinessType and generated obligation |
| Six-month rent instalments | Lease PaymentIntervalMonths; new shops default to 6, houses to 1 |
| Monthly equivalent and annual reports | Existing lease MonthlyRent; instalment = MonthlyRent × PaymentIntervalMonths |
| Single instalment spanning months | New rent payment months selector; total split to individual month records atomically with exact cent reconciliation |
| Six-month renewal | RenewalIntervalMonths default 6; renewal requires agreement and a renewed dated lease |
| Receiving bank and account | Existing landlord settings; source reference includes example details but live values are not overwritten |
| Notice of non-renewal | Existing NoticeDays, default 30 |
| No unauthorised subletting, nuisance or alterations; cleanliness, minor repairs and utilities | English generated contract obligations |
| Peaceful enjoyment | English landlord undertaking |
| Two separate witnesses | Existing witness fields identify tenant witness; new landlord witness fields and witness titles |
| Signed effective agreement | Generated signature/date lines for both parties and witnesses; electronic signature collection is not implemented |
| PDF contract | Existing contract print/save-PDF view |
| Historical contracts | Existing immutable snapshots are unchanged; generate a new snapshot for new terms |

## Setup for the supplied example

Set the shop name to Shop No. 06, enter its actual building number, full Magomeni address and lawful business activity. Set agreed monthly rent to 250000, payment interval to 6, renewal interval to 6 and notice to 30 days. Complete both witnesses' names and telephone numbers. Enter the landlord's verified receiving bank details in Settings. Existing leases retain a monthly payment interval until explicitly edited; they are not silently converted.

When entering the six-month payment, select the lease, first rental month and 6 months covered. Enter **1500000 as the total**, not as the monthly amount. Save creates six rent records of 250000 each, with distinct month allocations and an audit record per allocation, in one transaction. All allocated months must fall within the lease. Existing single-month payments and edits retain their existing workflow. Renewals and rent revisions must be recorded explicitly; this release does not automatically change agreed rents or extend leases.

The shop contract generation checks that the building number and names/telephone numbers of both witnesses are complete. New SQL Server fields are added by migration 20261007083000_ContractRequirements, through the existing startup auto-migration setting. No personal, bank or financial records are seeded or replaced.
