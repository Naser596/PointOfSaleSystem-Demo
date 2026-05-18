# BMAD ERP Sprint: Phases 3-4

Date: 2026-05-11
Scope: Functional ERP implementation slice for existing ASP.NET Core MVC/EF Core system.

## BMAD Phase 3: Inventory And Warehouse Depth

Full phase goal: make stock reliable across stores and warehouses.

This sprint slice:

- Add stock ledger page for recent stock movements.
- Add warehouse balance view with on-hand, reserved, available, and valuation.
- Add reorder suggestions from current stock and minimum stock thresholds.
- Keep transfer and adjustment posting unchanged.

Out of scope for this slice:

- Full stock count workflow.
- Batch/serial tracking.
- Reservation engine for sales orders.
- Transfer approval enforcement.

## BMAD Phase 4: Sales, CRM, And Receivables

Full phase goal: complete order-to-cash for B2B and retail.

This sprint slice:

- Add receivables page for open invoices.
- Add AR aging by customer.
- Show overdue invoice risk buckets.
- Link open receivable rows back to sales document details for payment follow-up.

Out of scope for this slice:

- Customer credit limits and terms fields.
- Delivery notes and fulfillment.
- Price lists.
- Automated reminders/email.

## Acceptance Criteria

- `/Warehouses/StockLedger` shows stock balances, recent movements, and reorder suggestions.
- `/SalesDocuments/Receivables` shows open invoice balances and customer aging.
- No schema migration is required for this slice.
- Existing warehouse transfer/adjustment and sales document workflows remain unchanged.
- `dotnet build` passes.
