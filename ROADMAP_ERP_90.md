# ERP 90% Roadmap

Qellimi i ketij roadmap-i eshte ta coje sistemin nga ERP MVP ne produkt te perdorshem serioz per kompani te vogla dhe te mesme. Payment providers si Stripe mbeten vetem te pergatitura per integrim, pa u lidhur tani.

## Faza 1 - Stabilitet financiar dhe dokumente

Status: prioriteti me i larte.

- Ndaj sakte revenue nga pipeline: Invoice, Credit Note, Quote, Order.
- Posto cdo obligation paid ne finance: payment record, bank transaction, journal entry.
- Shto transaction safety per pagesa dhe posting financiar.
- Ndal duplicate payroll per te njejten periudhe.
- Ndal duplicate conversion per sales documents.
- Forco Credit Note me vlere negative dhe reference te invoice burim.
- Shto reversal flow per obligations te paguara gabim.
- Shto PDF/print profesional per Quote, Order, Invoice, Credit Note, Purchase Order, Goods Receipt, Supplier Invoice.

## Faza 2 - Accounting profesional

Status: e domosdoshme per ERP 90%.

- Profit and Loss report.
- Balance Sheet report.
- Cashflow report.
- Accounts Receivable aging.
- Accounts Payable aging.
- Trial Balance.
- General Ledger per account.
- Period close/open controls.
- Tax summary report.
- Mapping i produkteve, taxave dhe expense types me chart of accounts.

## Faza 3 - Inventory dhe warehouse

Status: e rendesishme per kompani me mall fizik.

- Stock valuation report.
- Stock movement ledger per produkt.
- Reserved stock per sales orders.
- Reorder points dhe purchase suggestions.
- Multi-warehouse transfer approval.
- Goods receipt quality/status: Received, Inspected, Rejected.
- Product batch/serial tracking per produkte qe e kerkojne.
- Stock count/cycle count workflow.

## Faza 4 - Purchasing dhe supplier control

Status: e rendesishme per kontroll kostosh.

- Purchase requisition.
- Approval para purchase order.
- PO -> Goods Receipt -> Supplier Invoice three-way matching.
- Variance report: ordered, received, invoiced.
- Supplier balance dhe unpaid supplier invoices.
- Supplier statement.
- Purchase return/debit note.

## Faza 5 - Sales CRM dhe receivables

Status: e rendesishme per kompani B2B.

- Customer account statement.
- Customer credit limit.
- Overdue invoice reminders.
- Partial credit note.
- Sales order fulfillment status.
- Delivery note.
- Price lists per customer/group.
- Discount approval rules.

## Faza 6 - HR dhe payroll

Status: baza ekziston, duhet thelluar.

- Employee contract fields.
- Salary advances linked to employee.
- Payroll deductions and bonuses.
- Payroll taxes/contributions as separate obligations.
- Payslip print/PDF.
- Payroll payment posting per employee or grouped batch.
- Leave/absence records.
- Employee document attachments.

## Faza 7 - Approval dhe audit

Status: duhet bere me i lidhur me proceset reale.

- Approval rules per amount/type.
- Approval inbox per user.
- Required approval before posting high-value PO, supplier invoice, discounts, stock adjustment.
- Audit log per create/update/delete me before/after values.
- Export audit logs.
- Lock posted documents from editing except reversal.

## Faza 8 - Offline dhe sync

Status: baza ekziston per POS, duhet forcuar.

- Server-side pending sync records.
- Conflict handling UI.
- Duplicate sale prevention with client id uniqueness.
- Retry history.
- Admin sync dashboard across devices, jo vetem local browser.
- Offline product/cache refresh status.

## Faza 9 - SuperAdmin dhe platform operations

Status: baza ekziston, duhet produktizuar.

- Subscription lifecycle dashboard.
- Alerts per companies expiring soon.
- Auto-disable after grace period.
- Manual override with reason.
- Company health score: logins, sales activity, storage, errors.
- Platform revenue report.
- Tenant backup/export tools.

## Faza 10 - UI/UX produkt final

Status: duhet polish i madh per perceptim profesional.

- Sidebar ERP navigation me module groups.
- Global search.
- Consistent empty states.
- Advanced filters per tables.
- Saved views.
- CSV/PDF exports.
- Better mobile/responsive tables.
- Professional POS terminal layout.
- Dashboard charts per revenue, expenses, cashflow, stock alerts.

## Target per 90%

Per te arritur 90%, sistemi duhet te kete:

- Dokumente profesionale me print/PDF.
- Accounting reports kryesore.
- Sales, purchasing, warehouse dhe finance te lidhura me posting te qarte.
- HR/payroll me obligations dhe payslips.
- Approval rules reale.
- Audit logs te plota.
- Offline sync me konflikt dhe admin visibility.
- SuperAdmin subscription control te automatizuar.
- Test coverage per workflow-et kryesore.

Pas ketyre, projekti mund te quhet rreth 90% ERP per kompani te vogla/mesme. Pjesa 100% zakonisht kerkon integrime bankare, e-invoicing sipas shtetit, payment providers, advanced tax localization dhe custom workflows per industri specifike.
