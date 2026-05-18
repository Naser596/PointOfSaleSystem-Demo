# BMAD ERP Sprint: Phases 1-2

Date: 2026-05-11
Scope: Functional ERP implementation slice for existing ASP.NET Core MVC/EF Core system.

## BMAD Phase 1: Accounting And Financial Control

Full phase goal: make journal posting reliable enough for ERP financial reporting.

This sprint slice:

- Enforce fiscal period close/open rules during journal posting.
- Assign journal entries to an open fiscal period when one covers the posting date.
- Prevent duplicate posting for the same source document.
- Add supplier invoice AP posting to the accounting service.

Out of scope for this slice:

- Full account mapping UI.
- Full P&L/balance sheet/cashflow redesign.
- Reversal UI.
- Period close management UI beyond existing fiscal-period records.

## BMAD Phase 2: AP, Purchasing, And Supplier Control

Full phase goal: complete procure-to-pay.

This sprint slice:

- Allow supplier invoices to be posted into AP.
- Keep matched PO/GR context.
- Support both matched invoices and standalone supplier invoices.
- Add controller action for posting supplier invoices.

Out of scope for this slice:

- Supplier master CRUD.
- Purchase requisitions.
- Supplier payment batches.
- Full three-way tolerance configuration.

## Acceptance Criteria

- Supplier invoice posting creates a balanced journal entry.
- Posting the same supplier invoice twice returns the existing journal entry and does not duplicate lines.
- Posting in a closed fiscal period is rejected.
- Posting in an open fiscal period assigns `FiscalPeriodId`.
- Supplier invoice status changes to `Posted` after successful posting.
- `dotnet test` passes for the affected service tests.
