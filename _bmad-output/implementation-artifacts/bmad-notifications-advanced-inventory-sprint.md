# BMAD Notifications And Advanced Inventory Sprint

Date: 2026-05-15
Project: POS/ERP Brownfield Implementation

## Scope

Implemented requested post-roadmap BMAD depth items:

- Email/SMS notification outbox
- Invoice reminders
- Overdue customer reminders
- Subscription expiry notifications
- Stock count workflow
- Batch/serial tracking
- Transfer approval
- Reorder suggestion to purchase order

## Notifications Delivered

- Added `NotificationMessages` table and EF migration.
- Added `INotificationService` / `NotificationService`.
- Added `/Notifications` outbox UI.
- Company users can generate:
  - invoice reminders for invoices due within 7 days
  - overdue customer email reminders
  - overdue customer SMS reminders when customer phone exists
- SuperAdmin can generate subscription expiry notices for companies expiring within 7 days or already expired.
- Notifications can be marked `Sent` or `Cancelled`.
- Generation is idempotent per company/type/entity/channel/recipient.

## Advanced Inventory Delivered

- Added `ProductTraceLots` table for batch/serial tracking.
- Added batch/serial entry form and recent traceability table to `/Warehouses`.
- Added traceability listing to `/Warehouses/StockLedger`.
- Added `StockCounts` and `StockCountLines` tables.
- Added `/Warehouses/StockCounts`.
- Added stock count details page for counted quantities, variance reasons, and posting.
- Posting a stock count creates warehouse stock adjustments for variances.
- Added stock transfer approval path:
  - configure approval rule `StockTransfer` / `Transfer`
  - transfer is created as `PendingApproval`
  - manager approves under `/Approvals`
  - warehouse posts approved transfer from `/Warehouses`
- Added reorder-to-purchase-order:
  - low-stock products appear in `/Purchasing`
  - reorder suggestions in `/Warehouses/StockLedger` can create PO directly

## Verification

- `dotnet build` passed.
- Full test suite passed: 39/39.
- Docker rebuilt successfully.
- App returned HTTP 200.
- PostgreSQL table list confirms:
  - `NotificationMessages`
  - `ProductTraceLots`
  - `StockCounts`
  - `StockCountLines`

## Notes

- Email/SMS is currently an internal outbox, not an SMTP/SMS provider integration. This is intentional so reminders are auditable before connecting real providers.
- Batch/serial tracking records traceability metadata; strict per-sale batch consumption can be added later when POS line selection supports batch picking.
