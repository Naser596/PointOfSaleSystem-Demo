# Current Implementation Map

Date: 2026-05-11
Project: Full ERP System
Source: Local repository inspection plus installed BMAD configuration.

## Scope Notes

This is a brownfield ASP.NET Core MVC POS/ERP application. The repository already contains a substantial implementation and should be evolved in place. BMAD guidance installed in `_bmad` defines `planning_artifacts` as `_bmad-output/planning-artifacts` and marks PRD, architecture, epics/stories, and readiness checks as required planning outputs. The full BMAD workflow bodies are not present in this install, so this map follows the available BMAD artifact sequence and grounds decisions in the inspected code.

No application code was changed while creating this artifact.

## Technology And Runtime

| Area | Current implementation |
| --- | --- |
| Web stack | ASP.NET Core MVC on `.NET 8`, Razor views, Identity, EF Core |
| Database | PostgreSQL runtime provider via `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Auth | ASP.NET Core Identity with roles: `SuperAdmin`, `Admin`, `Manager`, `Accountant`, `Warehouse`, `HR`, `Cashier` |
| Multi-tenant model | Shared database with `CompanyId` on business entities and explicit company validation in `ApplicationDbContext.SaveChanges` |
| Deployment | `Dockerfile`, `docker-compose.yml`, `.env.example`, `Taskfile.yml`; app plus PostgreSQL |
| Reporting export | ClosedXML for Excel; custom `SimplePdfService` for basic PDFs |
| Tests | xUnit service/integration tests under `WebApplication3.Tests` for tenant isolation, accounting, sales workflow, warehouse, HR payroll, supplier invoice matching, reports, POS sale services, and auth flow |

## Repository Structure

| Path | Purpose |
| --- | --- |
| `Controllers/` | MVC controllers for POS, sales, products, users, ERP modules, platform admin, reports |
| `Models/` | EF entities and view models for POS, ERP core, HR, finance, operations, settings |
| `Services/` | Business workflow services for products, sales, inventory, accounting, warehouse, payroll, obligations, reports, subscriptions |
| `Data/` | EF `ApplicationDbContext` and seed data |
| `Views/` | Razor UI for POS, backoffice, accounting, purchasing, warehouse, HR, reports, SuperAdmin |
| `MigrationsPostgres/` | PostgreSQL EF migrations for current platform direction |
| `Migrations/` | Legacy migration snapshot plus deleted legacy migration files in git status |
| `WebApplication3.Tests/` | Automated tests and support fixtures |
| `docs/` | User guide and production readiness checklist |
| `_bmad/` | Installed BMAD config/help files |
| `_bmad-output/` | BMAD planning and implementation artifact directories |

## Implemented Business Modules

### Platform And Tenancy

Implemented:

- `Company` entity with display/legal details, branding fields, currency/tax defaults, invoice/receipt footer notes, access dates, grace period, and disable metadata.
- `ApplicationUser.CompanyId` association.
- SuperAdmin screens to create/edit companies, create company admin users, disable companies, and view subscription alerts.
- Hosted `CompanySubscriptionMonitorService` and `CompanySubscriptionService` to disable expired companies.
- Platform owner policy hardcoded to one SuperAdmin email.
- Company isolation validation in `ApplicationDbContext.SaveChanges` for most business entities and cross-company references.

Maturity: strong foundation, but platform operations are not fully productionized.

### Identity, Roles, And Access

Implemented:

- ASP.NET Core Identity with role-based authorization across controllers.
- Login/logout flow with audit logging and session checks.
- Company-active checks during cookie validation.
- SuperAdmin restriction to configured platform owner email.
- User management inside company context.

Maturity: usable RBAC, but not yet a permissions system and has production security gaps.

### POS And Retail Sales

Implemented:

- POS screen with product search/filter, barcode lookup, cart, customer selection, discount code handling, cash sale, simulated card sale, local offline sale queue, and sync endpoint.
- Sale creation updates product stock and writes stock movements.
- Sale item snapshots for product name, SKU, unit price, cost, tax, refund values.
- Sales list, details, receipt, daily export, and return workflow.
- Customer stats updated after sales.

Maturity: POS workflow is functional for MVP/SMB use. It needs transaction hardening, register closure, fiscal numbering, payment provider integration, and stronger offline conflict handling.

### Product, Inventory Basics, Customers, Discounts

Implemented:

- Product CRUD with category, SKU/barcode, cost price, sale price, tax rate, minimum stock, image upload, soft delete.
- Category CRUD and soft delete.
- Customer CRUD, search, statement view, and sales linkage.
- Discount CRUD, verification, percentage/fixed discount logic, usage count.
- Low-stock service and dashboard signals.

Maturity: solid POS catalog foundation. Missing supplier master, price lists, import/export, batch/serial, reserved stock workflow, and advanced pricing.

### Sales Documents

Implemented:

- Sales document entities for Quote, Order, Invoice, Credit Note.
- Create document, add lines, print/PDF, details, status update.
- Conversion flow: Quote -> Order/Invoice, Order -> Invoice, Invoice -> CreditNote.
- Duplicate conversion guard and negative credit note totals.
- Invoice payment recording with payment record, optional bank transaction, AR clearing journal entry.
- Accounting posting for issued/closed invoices and orders.

Maturity: good foundation for B2B documents. Needs fulfillment, delivery notes, partial credit notes, credit limits, lock/reversal rules, and stronger document lifecycle controls.

### Purchasing And Supplier Invoices

Implemented:

- Purchase order creation, lines, details, print/PDF.
- Goods receipt against purchase order, warehouse stock increase, product stock increase, stock movements, and inventory/AP journal entry.
- Supplier invoice creation with supplier details, line totals, optional PO and goods receipt matching.
- Match status: `Matched`, `PartiallyMatched`, `Unmatched`.

Maturity: procurement flow exists but is not a complete purchasing suite. Needs supplier master, requisitions, approval enforcement before receiving, supplier payments, AP posting lifecycle, variance controls, purchase returns, and debit notes.

### Warehouse And Stock

Implemented:

- Warehouses, stock locations, warehouse stock, transfers, and adjustments.
- Stock transfer service validates company/warehouse consistency and stock availability.
- Stock adjustment updates warehouse stock, product stock, and stock movement ledger.
- Goods receipts update warehouse stock and product stock.

Maturity: good multi-warehouse base. Missing stock count, reservations, batch/serial, reorder suggestions, quality inspection, movement ledger UI, and valuation controls.

### Finance, Financial Accounts, Obligations

Implemented:

- Financial accounts and bank transactions.
- Reconciliation status toggling.
- Company obligations for payroll, tax, rent, utilities, debt, unpaid invoice, advance, and other.
- Mark obligation paid with payment record, optional bank transaction, and journal posting.
- Finance dashboard with sales, refunds, supplier invoices, payroll/obligations, gross/net profit estimates, and monthly profit points.

Maturity: operational finance is partially connected. Missing full AP payment cycle, bank import/reconciliation matching, cashflow statement by source, budgeting, recurring obligations, and reversal workflow.

### Accounting

Implemented:

- Chart of accounts.
- Fiscal periods.
- Journal entries and lines.
- Balanced journal entry creation service.
- Trial balance, profit and loss, and balance sheet rollups from posted journal lines.
- Automatic postings for sales documents, payments, goods receipts, and paid obligations.

Maturity: accounting foundation exists. Missing period close/open enforcement, complete source posting coverage, tax mappings, manual journal UI depth, audit lock controls, retained earnings, and accountant-grade statements.

### HR And Payroll

Implemented:

- Employees with basic personal, job, department, salary, due day, active status.
- Payroll runs with selected employee lines, bonus, deduction, tax, gross/net amounts.
- Payroll lines create linked payroll obligations per employee.
- Update/delete payroll line/run controller actions exist.

Maturity: payroll MVP exists. Missing contracts, leave/absence, advances, separate statutory tax/contribution obligations, payslips, payment batches, duplicate period controls, and HR documents.

### Approvals And Audit

Implemented:

- Approval requests and approval rules.
- Purchase order amount threshold creates pending approval request.
- Approval inbox and rule toggling.
- Audit log with action, entity, user, company, IP, summary, before/after JSON capability.
- Audit log filtering and Excel export.

Maturity: base entities and UI exist. Approvals are not consistently enforced across high-risk actions. Audit logging is present but not universally before/after and not immutable/retention-ready.

### Offline Sync

Implemented:

- Browser localStorage pending sale queue in POS.
- `/POS/SyncPendingSales` endpoint.
- `OfflineSyncRecord` table with client id uniqueness per company, payload, status, error, sale id.
- Offline sync dashboard.

Maturity: useful POS offline queue base. Missing admin cross-device remediation, conflict UI, retry history depth, cached product versioning, device identity, and stock conflict resolution.

### Reporting And Documents

Implemented:

- ERP report builder with net sales, AR/AP, inventory valuation, purchase variance, customer statements, cash in/out, payroll, gross/net profit.
- Excel export with summary, AR aging, inventory, customer statements.
- Accounting rollups.
- Print/PDF for sales documents, purchase orders, supplier invoices, receipts.

Maturity: broad reporting MVP. Missing advanced filters, saved views, scheduled reports, full ledger drilldowns, tax reports, export consistency, and dashboard chart polish.

## Current Implementation Level

Estimated implementation maturity by area:

| Area | Approximate maturity | Notes |
| --- | ---: | --- |
| POS retail sales | 75% | Works as POS MVP; needs transaction, close, offline conflict, payments |
| Multi-company platform | 70% | Good shared-DB tenant base; needs ops/security/productization |
| Sales documents | 65% | Core flow exists; needs lifecycle, fulfillment, controls |
| Purchasing/AP | 55% | PO/GR/SI exists; supplier/payment/variance missing |
| Warehouse | 55% | Multi-warehouse base; advanced inventory missing |
| Accounting | 50% | Posting and reports started; close/tax/ledger depth missing |
| Finance/cash | 50% | Accounts/payments exist; reconciliation and AP cycle incomplete |
| HR/payroll | 45% | Payroll MVP; HR/payroll compliance missing |
| Approvals/audit | 45% | Base exists; enforcement and immutability missing |
| Offline sync | 40% | Browser queue exists; conflict/admin controls missing |
| Production operations | 35% | Docker/docs exist; secrets, HTTPS, logs, CI, backups, health need work |

## Key Brownfield Constraints

- Preserve the MVC + EF Core architecture unless a specific module benefits from a scoped refactor.
- Keep the PostgreSQL shared-database multi-tenant approach and strengthen it rather than introducing per-tenant databases now.
- Move business-critical logic out of controllers gradually into services; avoid a rewrite.
- Treat existing tests as regression assets and expand them around the most risky workflows.
- Do not add ERP modules as isolated screens only; connect them to accounting, audit, permissions, and reports.
