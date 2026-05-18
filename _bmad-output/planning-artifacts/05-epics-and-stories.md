# Epics And Stories For Remaining Work

Date: 2026-05-11
Project: Full ERP System

## Story Format

Each story is written for brownfield implementation in the existing ASP.NET Core MVC/EF Core codebase. Stories should be implemented incrementally with tests. Acceptance criteria are intentionally concrete enough to support BMAD sprint planning.

## Epic 1: Production Foundation And Security

Goal: Make the existing system safe to deploy for controlled production pilots.

### Story 1.1: Environment-Driven Platform Owner Bootstrap

As a platform operator, I want SuperAdmin bootstrap credentials configured through environment/secrets so that no default production password exists in code.

Acceptance criteria:

- SeedData no longer creates a known default password in production.
- Platform owner email is configurable.
- Missing bootstrap configuration fails clearly in production.
- Tests cover development and production bootstrap behavior.

### Story 1.2: Production Authentication Hardening

As a platform operator, I want strong auth defaults so that accounts are protected from weak passwords and brute-force login.

Acceptance criteria:

- Production password policy is stronger than development defaults.
- Lockout/rate limiting is enabled for failed login.
- Secure cookie settings are production-aware.
- Login failure audit events are recorded.

### Story 1.3: HTTPS And Reverse Proxy Safety

As a hosted ERP user, I want all sessions protected by HTTPS so that credentials and business data are not exposed.

Acceptance criteria:

- HTTPS redirection is enabled for production.
- Forwarded headers are configured for reverse proxy deployment.
- HSTS and secure cookies are documented and tested in production config.

### Story 1.4: Controlled Migration And Release Pipeline

As a developer/operator, I want database migrations run as a deploy step so that app startup does not unexpectedly mutate production schema.

Acceptance criteria:

- Production startup does not automatically run migrations.
- A migration command/runbook exists.
- CI builds the app and test project.
- Migration scripts are validated before release.

## Epic 2: Tenant Isolation And Permissions

Goal: Guarantee company data separation and move toward fine-grained ERP permissions.

### Story 2.1: Cross-Tenant Route Test Suite

As a platform owner, I want automated tests for cross-company access so that one company cannot read or mutate another company's data.

Acceptance criteria:

- Tests cover core controllers: Products, Customers, Sales, POS, SalesDocuments, Purchasing, Warehouses, HR, Finance, Reports.
- Form posts using another company ID return forbidden/not found and do not modify data.
- Intentional SuperAdmin exceptions are documented.

### Story 2.2: Permission Catalog

As a company admin, I want permissions beyond broad roles so that staff can be assigned precise capabilities.

Acceptance criteria:

- Permission constants are defined for high-risk actions.
- Role-to-permission mapping exists per company or globally.
- Controllers enforce permissions server-side for at least inventory adjust, PO approve, accounting post, user manage, reports export.

### Story 2.3: Approval Enforcement Layer

As a manager, I want approval rules to block sensitive actions until approved so that controls are real, not only informational.

Acceptance criteria:

- Pending approval blocks configured actions.
- Purchase order receive/invoice is blocked when PO approval is pending.
- Stock adjustment, high discount, supplier invoice, and high-value payment can require approval.
- Audit logs show request, approval/rejection, and final action.

## Epic 3: Transactional POS And Store Operations

Goal: Make retail sales reliable under stock, payment, offline, and register constraints.

### Story 3.1: Transactional POS Sale Service

As a cashier, I want sale creation to succeed or fail as one unit so that stock, discount usage, sale items, customer stats, and audit never become inconsistent.

Acceptance criteria:

- POS sale creation is moved into a service.
- Sale, sale items, stock decrement, stock movements, discount usage, customer stats, and audit are transactionally consistent.
- Insufficient stock under concurrent sale attempts is handled safely.
- Tests cover rollback when a later step fails.

### Story 3.2: Register Session Close

As a cashier, I want to close my register session with counted cash so that daily cash variance is controlled.

Acceptance criteria:

- Open session records starting cash, register, user, and time.
- Close session records counted cash, expected cash, variance, notes.
- Variance over threshold requires approval.
- Reports can filter by register/session.

### Story 3.3: POS Refund Posting

As an admin, I want refunds to create payment and accounting reversals so that sales, cash, inventory, and reports remain correct.

Acceptance criteria:

- Return workflow creates refund payment record when money is returned.
- Stock return is optional based on condition.
- Journal entries reverse revenue/tax/cash/COGS as appropriate.
- Duplicate/over-quantity returns are rejected.

### Story 3.4: Offline Sync Conflict Handling

As a store manager, I want failed offline sales to show conflicts clearly so that I can resolve them without database access.

Acceptance criteria:

- Offline sync records keep retry history.
- Duplicate client IDs are idempotent.
- Stock conflicts are marked with resolvable status.
- Admin UI allows retry, cancel, or adjust-and-post with audit.

## Epic 4: Accounting Completion

Goal: Make the existing accounting foundation reliable for SMB financial reporting.

### Story 4.1: Posting Matrix And Account Mapping

As an accountant, I want configurable account mappings so that each business event posts to the right ledger accounts.

Acceptance criteria:

- Mapping exists for product revenue, COGS, inventory, tax, AP, AR, payroll, expenses, cash/bank.
- Default mappings are created for new companies.
- Missing required mapping blocks posting with a clear message.

### Story 4.2: Supplier Invoice AP Posting

As an accountant, I want supplier invoices to post AP and expenses/inventory so that payables are accurate.

Acceptance criteria:

- Supplier invoice status can be posted.
- Posting creates balanced journal entries.
- Duplicate posting is prevented.
- Posted supplier invoices are locked except through reversal/debit note.

### Story 4.3: Fiscal Period Close

As an accountant, I want closed periods to block postings so that prior reports cannot change silently.

Acceptance criteria:

- Fiscal period can be opened/closed by authorized users.
- Posting dates in closed periods are rejected.
- Reopening requires permission and audit reason.
- Reports display current period status.

### Story 4.4: General Ledger Drilldown

As an accountant, I want to drill into account balances so that I can explain every total.

Acceptance criteria:

- General ledger page filters by account/date/source.
- Rows link to source documents where available.
- Debit, credit, and running balance are shown.
- Export to Excel is available.

## Epic 5: Procure-To-Pay

Goal: Complete purchasing from supplier master through payment.

### Story 5.1: Supplier Master

As a purchasing user, I want supplier profiles so that supplier data is not retyped on every document.

Acceptance criteria:

- Supplier CRUD with tax number, contact, address, email, phone, payment terms.
- POs and supplier invoices can select a supplier.
- Existing free-text supplier fields remain supported during migration.

### Story 5.2: Purchase Requisition To PO

As a department user, I want to request purchases before a PO is created so that spending can be approved earlier.

Acceptance criteria:

- Purchase requisition with lines, requester, department/cost center, needed date.
- Approval flow before conversion.
- Approved requisition converts to PO.

### Story 5.3: Three-Way Match Tolerances

As an accountant, I want PO, goods receipt, and supplier invoice variances detected so that overbilling and receiving errors are controlled.

Acceptance criteria:

- Variance by quantity and value is calculated.
- Tolerance rules are configurable.
- Blocking or warning behavior is supported.
- Variance report is exportable.

### Story 5.4: Supplier Payment Batch

As an accountant, I want to pay supplier invoices in batches so that AP payment runs are efficient.

Acceptance criteria:

- Open supplier invoices can be selected.
- Partial/full payment is supported.
- Payment creates payment records, bank transactions, and AP clearing journal entries.
- Supplier statement reflects payment.

## Epic 6: Inventory And Warehouse Control

Goal: Make inventory reliable, auditable, and useful for purchasing and sales decisions.

### Story 6.1: Stock Movement Ledger

As a warehouse manager, I want a stock ledger per product so that I can trace every stock change.

Acceptance criteria:

- Ledger filters by product, warehouse, location, movement type, date.
- Opening, movement, and closing quantities are shown.
- Source documents link where available.
- Export is available.

### Story 6.2: Stock Reservations

As a sales user, I want sales orders to reserve stock so that committed stock is not oversold.

Acceptance criteria:

- Sales order can reserve stock per warehouse.
- Available stock equals on-hand minus reserved.
- Invoice/delivery consumes reservation.
- Cancel/reject releases reservation.

### Story 6.3: Reorder Suggestions

As a buyer, I want reorder suggestions so that low-stock products become purchase recommendations.

Acceptance criteria:

- Product reorder point and preferred supplier are supported.
- Report suggests purchase quantities.
- Suggestions can create purchase requisitions or POs.

### Story 6.4: Stock Count Workflow

As a warehouse manager, I want stock counts so that physical inventory can be reconciled.

Acceptance criteria:

- Create stock count by warehouse/location.
- Enter counted quantities.
- Differences require reason and optional approval.
- Posting count creates stock adjustments and audit logs.

## Epic 7: Sales, CRM, And Receivables

Goal: Complete B2B sales workflows and customer credit control.

### Story 7.1: Customer Credit Limits And Terms

As a manager, I want credit limits and payment terms so that risky sales are controlled.

Acceptance criteria:

- Customer has credit limit and payment term fields.
- New invoice/order checks open balance against limit.
- Override requires permission/approval.

### Story 7.2: Delivery Notes And Fulfillment

As a sales/warehouse user, I want delivery notes so that sales order fulfillment is tracked.

Acceptance criteria:

- Sales order can create delivery note.
- Delivered quantities are tracked by line.
- Fulfillment status updates automatically.
- Delivery note can print/PDF.

### Story 7.3: Overdue Invoice Reminders

As an accountant, I want overdue reminders so that customer collections are trackable.

Acceptance criteria:

- Overdue invoices are listed by aging bucket.
- Reminder notes/actions are recorded.
- Email reminder template can be generated.
- Customer statement includes reminder history.

### Story 7.4: Price Lists

As a company admin, I want price lists by customer/group so that B2B pricing is controlled.

Acceptance criteria:

- Customer groups and price lists exist.
- Sales document line price resolves from price list when applicable.
- Manual override requires permission.

## Epic 8: HR And Payroll Completion

Goal: Move HR/payroll from MVP to auditable business operations.

### Story 8.1: Employee Contracts And Documents

As an HR user, I want employee contracts and files so that employee records are complete.

Acceptance criteria:

- Contract fields are added: type, start/end, salary basis, work hours.
- Attachments can be uploaded to employee records.
- Attachment access is role/permission controlled.

### Story 8.2: Payroll Duplicate Period Guard

As an HR/payroll user, I want duplicate payroll runs prevented so that employees are not paid twice accidentally.

Acceptance criteria:

- System detects overlapping payroll periods for same employee.
- Duplicate run requires explicit override permission and reason.
- Tests cover duplicate prevention and allowed override.

### Story 8.3: Payslip PDF

As an employee/admin, I want payslips so that payroll details can be shared and archived.

Acceptance criteria:

- Payslip includes employee, period, gross, bonus, deductions, tax, net.
- PDF/print is available per employee line.
- Payslip generation is audited.

### Story 8.4: Payroll Payment Batch

As an accountant, I want payroll payment batches so that employee payments post consistently.

Acceptance criteria:

- Open payroll obligations can be selected.
- Payment batch creates payment records and bank transactions.
- Journal entries clear payroll payable/cash.

## Epic 9: Reporting, BI, And UX

Goal: Make reporting and UI professional enough for daily ERP use.

### Story 9.1: ERP Navigation Redesign

As a user, I want navigation grouped by ERP domain so that I can find work quickly.

Acceptance criteria:

- Sidebar groups modules by Sales, Purchasing, Inventory, Finance, HR, Platform, Reports.
- Role/permission controls visible menu items.
- POS remains optimized for cashier workflow.

### Story 9.2: Report Filters And Saved Views

As a manager, I want saved report filters so that recurring analysis is fast.

Acceptance criteria:

- Common reports support date, warehouse, customer, supplier, user, status filters where relevant.
- Users can save named views.
- Saved views are company-scoped.

### Story 9.3: Dashboard Snapshot Performance

As an admin, I want dashboards to load quickly as data grows.

Acceptance criteria:

- Daily/monthly summary snapshots exist for high-volume metrics.
- Dashboard can use snapshot data.
- Snapshot rebuild job is available.

### Story 9.4: Consistent Export Framework

As a business user, I want consistent exports so that data can be shared with accountants and managers.

Acceptance criteria:

- Standard CSV/Excel export pattern exists.
- Sensitive exports are audited.
- Reports use consistent filenames and columns.

## Epic 10: SaaS Platform Operations

Goal: Productize company lifecycle management for a multi-tenant ERP platform.

### Story 10.1: Subscription Plans And Limits

As a platform owner, I want plans and limits so that companies can be sold and managed consistently.

Acceptance criteria:

- Plan entity supports user/store/register limits and status.
- Company links to a plan.
- Limit violations warn or block based on configuration.

### Story 10.2: Company Health Dashboard

As a platform owner, I want tenant health signals so that I can support customers proactively.

Acceptance criteria:

- Dashboard shows login activity, sales activity, failed syncs, storage usage, errors, subscription status.
- Companies can be filtered by risk/health.

### Story 10.3: Tenant Export And Backup

As a platform owner, I want tenant export tools so that customer data can be backed up or handed over.

Acceptance criteria:

- Export key tenant data by company.
- Export action is audited.
- File storage references are included.

### Story 10.4: Platform Revenue Report

As a platform owner, I want platform revenue reporting so that SaaS business performance is visible.

Acceptance criteria:

- Platform subscription charges can be recorded.
- Revenue report groups by month/company/plan/status.
- Suspended/expired companies are visible.
