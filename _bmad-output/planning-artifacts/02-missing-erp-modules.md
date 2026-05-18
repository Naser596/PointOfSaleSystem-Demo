# Missing ERP Modules

Date: 2026-05-11
Project: Full ERP System

## Summary

The current system is beyond a POS MVP and already has ERP foundations: sales documents, purchase orders, warehouse stock, financial accounts, journals, HR/payroll, approvals, audit, offline sync, and SuperAdmin tenancy. The remaining gap is not one large rewrite. It is a set of missing modules and incomplete business workflows that must be connected to the existing data model and services.

## Module Gap Matrix

| ERP capability | Current state | Missing work | Priority |
| --- | --- | --- | --- |
| Supplier management | Supplier details are repeated on POs and supplier invoices | Supplier master, contacts, addresses, tax/VAT, payment terms, supplier statement, balance, default accounts | High |
| Purchase requisitions | Not implemented | Internal requisition, approval, conversion to PO, budget/threshold checks | Medium |
| Three-way matching | PO, goods receipt, supplier invoice links exist | Quantity/value variance checks, blocking rules, tolerance rules, variance reports, status automation | High |
| Accounts payable | Supplier invoices exist but payment lifecycle is incomplete | AP posting, due tracking, partial payments, supplier aging, payment batches, debit notes, reversals | High |
| Accounts receivable | Invoice payment exists; AR aging exists | Credit limits, overdue reminders, collection notes, partial credit notes, delivery/fulfillment status | High |
| General ledger | Journals and reports exist | Manual journal workflow, period close/open enforcement, source document locks, retained earnings, full ledger drilldown | High |
| Tax | Basic product/company tax rate exists | Tax codes, tax mapping to accounts, tax summaries, VAT/GST localization, exempt/reverse-charge support | High |
| Bank reconciliation | Bank transactions and reconcile toggle exist | Bank import, matching, reconciliation statements, unreconciled exception workflow | Medium |
| Inventory control | Warehouse/location/stock exists | Reservations, reorder points, purchase suggestions, stock count/cycle count, movement ledger UI, valuation method | High |
| Batch/serial tracking | Not implemented | Batch/lot/serial numbers, expiry dates, traceability, return linkage | Medium |
| Quality control | Not implemented | Goods receipt statuses: Received, Inspected, Rejected; quarantine stock | Medium |
| Sales fulfillment | Sales docs exist | Delivery notes, picking/packing, shipped/fulfilled statuses, stock reservation from sales orders | High |
| CRM | Customers exist | Leads/opportunities, customer groups, price lists, activities, reminders, credit terms | Medium |
| Pricing | Product price and discount codes exist | Customer/group price lists, volume pricing, discount approval, promotions calendar | Medium |
| POS operations | Stores/registers/sessions exist | End-of-day close, cash count, variance approval, receipt numbering, device/register identity | High |
| Payments | Manual cash/card and payment record exist | Payment provider integration, refunds, partial payments everywhere, payment status webhooks, receipt settlement | Medium |
| HR core | Employee profile exists | Contracts, documents, leave/absence, departments/positions, onboarding/offboarding | Medium |
| Payroll compliance | Payroll run exists | Payslips, advances, tax/contribution obligations, payment batches, duplicate period prevention | High |
| Fixed assets | Not implemented | Asset register, depreciation, disposal, asset accounting postings | Low/Medium |
| Budgeting | Not implemented | Department/account budgets, budget vs actual, purchase budget checks | Low/Medium |
| Notifications | Not implemented beyond UI alerts | Email/in-app notifications for low stock, approvals, overdue invoices, subscriptions, sync failures | Medium |
| Document management | Attachment entity exists | Upload UI, storage strategy, attachment permissions, virus scanning, retention | Medium |
| Import/export | Exports exist for some reports | Product/customer/supplier imports, standard CSV/Excel exports for all master data | Medium |
| Workflow/permissions | Role-based access exists | Fine-grained permissions, approval enforcement, maker-checker controls | High |
| Platform billing | Access dates and subscription alerts exist | Subscription plans, platform invoices, payment tracking, usage limits, tenant health score | Medium |
| Integrations | Not implemented | Accounting export, e-invoicing, bank feeds, payment provider, tax authority/local fiscal integrations | Later, country-dependent |
| BI/analytics | Dashboards and report builder exist | Saved filters, drilldowns, scheduled reports, KPI comparisons, snapshots/materialized reports | Medium |

## Missing Cross-Cutting Capabilities

### Workflow Enforcement

Several modules have tables and screens but the rules are not enforced end-to-end. Examples:

- Purchase orders can enter `PendingApproval`, but all downstream actions must consistently block until approved.
- Posted financial documents should not be edited directly; correction should happen through reversals, returns, credit/debit notes.
- Fiscal period status exists, but transactions are not blocked for closed periods.
- Approval rules exist but are only partially connected to business workflows.

### Accounting Coverage

The application has a journal engine, but not every business event produces complete accounting entries. Missing coverage includes:

- Supplier invoice posting and AP recognition separate from goods receipt.
- POS sale posting to revenue, tax, cash/card, and COGS.
- Returns/refunds posting.
- Stock adjustment gains/losses.
- Payroll accruals versus payments.
- Bank fees, transfers, reconciliation adjustments.

### Master Data

The system needs stronger master data before it can behave like a full ERP:

- Supplier master.
- Tax codes and account mappings.
- Product account mappings.
- Customer payment terms and credit limits.
- Price lists.
- Units of measure.
- Warehouse zones/bin structure beyond simple stock locations.
- Departments/cost centers.

### Compliance And Localization

Full ERP readiness depends heavily on target market. Current implementation is generic. Missing country-specific work likely includes:

- VAT/GST rules and reporting.
- Fiscal receipt/e-invoicing requirements.
- Payroll tax/contribution rules.
- Legal invoice numbering and correction rules.
- Data retention rules.

## Recommended Module Order

1. Accounting completion and document locks.
2. AP/AR lifecycle completion.
3. Inventory reservations, counts, and valuation.
4. Purchasing supplier master and three-way matching.
5. Sales fulfillment and credit control.
6. POS end-of-day close and payment/refund hardening.
7. HR/payroll compliance basics.
8. Platform operations, billing, and tenant health.
9. Notifications, imports/exports, integrations.
10. Industry/country-specific extensions.
