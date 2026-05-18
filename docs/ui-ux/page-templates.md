# ERP/SaaS Page Templates

Date: 2026-05-11
Scope: Existing Razor views and Bootstrap UI patterns.

## Purpose

This document defines reusable page templates for the existing POS/ERP system. These templates should guide future UI work without changing business logic. They map to the current project structure under `Views/`.

## Global Page Frame

Use this frame for most authenticated backoffice pages:

1. Application shell from `Views/Shared/_Layout.cshtml`.
2. `.page-header` with title, description, and actions.
3. Optional alert/notice band for urgent workflow items.
4. Optional filter/search toolbar.
5. Main content grid: KPI cards, forms, tables, charts, or document panels.
6. Empty/loading/error states inside the affected card or table area.

Recommended page header structure:

```html
<div class="page-header d-flex justify-content-between align-items-start gap-3">
  <div>
    <h1><i class="fas fa-... me-3"></i>Page Title</h1>
    <p>One clear sentence explaining the work area.</p>
  </div>
  <div class="d-flex gap-2 flex-wrap justify-content-end">
    <a class="btn btn-primary">Primary Action</a>
    <a class="btn btn-outline-secondary">Secondary</a>
  </div>
</div>
```

## Template 1: Executive Dashboard

Current examples:

- `Views/Home/Index.cshtml`
- `Views/Erp/Index.cshtml`
- `Views/SuperAdmin/Index.cshtml`

Use for:

- Company dashboard.
- ERP core overview.
- Platform overview.

Layout:

1. Page header.
2. Command center or urgent alerts.
3. KPI grid, max 4 primary cards in the first row.
4. Charts and ranked lists.
5. Recent activity tables.
6. Quick actions, if needed, after metrics.

Rules:

- First viewport should answer: sales/cash/profit/stock risk/pending work.
- Do not overload dashboard with 16 large stat cards. Group secondary metrics into compact cards or tables.
- Use chart cards for trends and comparisons only.
- Put urgent obligations, low stock, failed sync, and pending approvals above generic quick actions.

Recommended dashboard sections:

- Business command center: current date range, net revenue, profit, stock risk, pending obligations.
- KPI row: revenue, profit, cash, alerts.
- Trend chart: sales/profit over time.
- Work queues: low stock, overdue obligations, pending approvals, failed sync.
- Recent activity: sales, documents, journal entries.

Responsive:

- Desktop: 4 KPI columns, chart/list split 8/4 or 7/5.
- Tablet: 2 KPI columns.
- Mobile: 1 column, alerts before charts.

## Template 2: List + Create Page

Current examples:

- `Views/SalesDocuments/Index.cshtml`
- `Views/Purchasing/Index.cshtml`
- `Views/FinancialAccounts/Index.cshtml`
- `Views/Warehouses/Index.cshtml`
- `Views/Approvals/Index.cshtml`

Use for:

- Pages where users create a simple record and immediately see recent records.

Layout:

1. Page header with secondary navigation.
2. Create card.
3. Optional filter/search bar.
4. Data table card.

Rules:

- Embedded create forms should stay short.
- If form exceeds one row plus notes, consider moving create to a dedicated page.
- Table action buttons must be consistent: View, Edit, Print/PDF, Delete.
- Place dangerous actions after safe actions.

Create form layout:

- 2-4 columns on desktop.
- Full width fields on mobile.
- Submit button rightmost or full-width if embedded.

Table layout:

- Entity identifier first.
- Status and dates in middle.
- Money right aligned.
- Actions last, right aligned.

## Template 3: Master Data Table Page

Current examples:

- `Views/Products/Index.cshtml`
- `Views/Customers/Index.cshtml`
- `Views/Users/Index.cshtml`
- `Views/Categories/Index.cshtml`
- `Views/Discounts/Index.cshtml`

Use for:

- Products, customers, users, suppliers later, chart of accounts later.

Layout:

1. Page header with Add action.
2. Filter sidebar or top filter toolbar.
3. Main table.
4. Pagination if record count grows.

Products-specific layout:

- Current left category sidebar is acceptable on desktop.
- On tablet/mobile, category sidebar should become horizontal chips or a collapsible filter panel.
- Product table should keep image/name/SKU/category/cost/price/margin/tax/stock/actions.

Search rules:

- Debounce live search at 250-400ms.
- Show loading state in table area.
- Preserve search/category in URL in future redesign.
- No-results state should offer "Clear filters" and "Add Product" when authorized.

Action rules:

- Icon-only table buttons must get `aria-label`.
- Destructive delete uses SweetAlert/confirmation.
- Bulk actions can be added later with checkbox column and action bar.

## Template 4: Detail + Workflow Page

Current examples:

- `Views/SalesDocuments/Details.cshtml`
- `Views/Purchasing/Details.cshtml`
- `Views/Sales/Details.cshtml`
- `Views/Products/Details.cshtml`

Use for:

- Documents and entities with related lines/actions.

Layout:

1. Page header with entity number/name and status.
2. Primary action bar: Print, PDF, Back, Convert/Post/Pay.
3. Main two-column grid:
   - Left: line table or main details.
   - Right: summary/status card.
4. Workflow cards below: add line, payment, goods receipt, activity.
5. Related records: receipts, payments, journal entries, audit trail later.

Rules:

- Status should appear in title area and summary card.
- Summary card should show key dates, totals, paid/balance, source references.
- Workflow actions must be visually separated from read-only details.
- Posted/closed documents should show lock status once business logic supports it.

Responsive:

- Main detail grid collapses to single column.
- Summary card should appear before long line tables on mobile only when it contains primary action.

## Template 5: Document / Invoice Page

Current examples:

- `Views/SalesDocuments/Print.cshtml`
- `Views/SupplierInvoices/Details.cshtml`
- `Views/SupplierInvoices/Print.cshtml`
- `Views\Sales\Receipt.cshtml`
- `Views\Purchasing\Print.cshtml`

Use for:

- Invoice, quote, order, credit note, purchase order, supplier invoice, receipt.

Screen view structure:

1. Page header with document number and actions.
2. Document surface card.
3. Company block.
4. Customer/supplier block.
5. Document metadata block.
6. Lines table.
7. Totals panel.
8. Notes/footer.

Print/PDF structure:

- Remove app navigation.
- Use white background.
- Use black/slate text.
- Keep borders thin and consistent.
- Avoid dark-mode colors in print output.
- Show company logo at restrained size.

Document status badges:

- Draft: secondary.
- Issued/Posted: primary.
- Accepted/Received/Paid/Synced: success.
- PendingApproval/PartiallyPaid/PartiallyReceived: warning.
- Cancelled/Rejected/Failed/Overdue: danger.

Invoice table rules:

- Description left.
- Quantity, unit price/cost, tax, total right aligned.
- Totals panel no wider than 420px on desktop.
- Balance due should be visually stronger than subtotal.

## Template 6: Finance / Accounting Page

Current examples:

- `Views/Finance/Index.cshtml`
- `Views/Accounting/Index.cshtml`
- `Views/FinancialAccounts/Index.cshtml`
- `Views/Obligations/Index.cshtml`
- `Views/Reports/Index.cshtml`

Use for:

- Financial dashboards, ledgers, reports, obligations, bank/cash pages.

Layout:

1. Page header.
2. Date/status filter toolbar.
3. KPI row.
4. Main chart/summary split.
5. Tables grouped by accounting concept.

Rules:

- Money values right aligned.
- Negative values use minus sign and danger color.
- Totals rows use `table-light`/surface-secondary and bold text.
- Accounting reports should prefer dense tables over decorative visuals.
- Every report table should have export path eventually.

Recommended report sections:

- Profit snapshot.
- Cashflow snapshot.
- Balance snapshot.
- AR/AP aging.
- Inventory valuation.
- Purchase variance.
- Journal drilldowns.

Charts:

- Revenue/profit: grouped bar or line.
- Sales trend: line.
- Payment split: doughnut only if categories are few.
- Top products/users: sorted bar or ranked table.

## Template 7: Inventory / Warehouse Operations Page

Current examples:

- `Views/Warehouses/Index.cshtml`
- `Views/Products/Index.cshtml`
- `Views/Purchasing/Details.cshtml` goods receipt section.
- `Views/Reports/Index.cshtml` inventory valuation.

Use for:

- Warehouse, stock, product, transfers, adjustments.

Layout:

1. Page header.
2. Operational forms in two-column cards.
3. Current stock table.
4. Reference lists: warehouses, locations, movements.

Rules:

- Inventory-changing forms require clear labels and reason fields.
- Quantity delta fields must visually explain positive/negative impact.
- Low stock should show badge + icon + current/min values.
- Stock tables need warehouse and location visible.

Future stock ledger template:

- Filter toolbar: product, warehouse, location, date, movement type.
- Ledger table: date, source, movement type, in, out, balance, user.
- Summary: opening, movement total, closing.

## Template 8: POS Terminal

Current example:

- `Views/POS/Index.cshtml`

Use for:

- Cashier operational selling.

Layout:

Current structure is good:

- Product/search panel left.
- Sticky cart/payment panel right.
- Category strip.
- Offline queue banner.
- Payment modal.

Rules:

- POS is allowed to be more task-focused and less dense than backoffice.
- Search input should be the dominant control.
- Cart total must remain visible.
- Payment buttons need large touch targets.
- Offline state must be obvious and recoverable.
- Do not hide critical cashier actions in dropdowns.

Responsive:

- Desktop: two-column terminal.
- Tablet/mobile: products first, cart follows; consider sticky checkout bar later.
- Buttons minimum 44px height.

## Template 9: Modal Workflow

Current examples:

- POS payment modal.
- HR edit employee modal.
- Obligations add modal.
- SweetAlert product delete.

Use for:

- Short workflows.

Structure:

1. Modal header: title + icon.
2. Body: short context and form.
3. Footer: Cancel + primary/destructive action.

Rules:

- Do not use modals for long multi-section forms.
- `modal-lg` or `modal-xl` only when necessary.
- Long modal content must scroll internally and keep footer visible if possible.
- Destructive confirmation text must name the record.

## Template 10: State Templates

### Loading

Use:

- Global `#loadingOverlay` for blocking submit/post/payment actions.
- Local table loading for filter/search updates.
- Skeleton rows later for reports/tables.

Copy:

- "Loading products..."
- "Running report..."
- "Posting payment..."
- "Syncing offline sales..."

### Empty

Use `.empty-state`.

Structure:

```html
<div class="empty-state">
  <i class="fas fa-box-open fa-3x mb-3"></i>
  <h2>No records yet</h2>
  <p>Short explanation.</p>
  <a class="btn btn-primary">Primary action</a>
</div>
```

### Error

Use:

- Inline validation for fields.
- Alert card for page errors.
- Toast for transient action failures.

Copy should say:

- What failed.
- Why, if known.
- What the user can do next.

Avoid raw exception details in UI.

## Page Group Mapping

| Page group | Template |
| --- | --- |
| Dashboard, ERP Core, SuperAdmin | Executive Dashboard |
| Products, Customers, Users, Categories, Discounts | Master Data Table |
| SalesDocuments, Purchasing, FinancialAccounts, Approvals | List + Create |
| SalesDocument Details, Purchase Details | Detail + Workflow |
| SupplierInvoice Details, Print pages, Receipt | Document / Invoice |
| Finance, Accounting, Reports, Obligations | Finance / Accounting |
| Warehouses, Product stock, Goods Receipt | Inventory / Warehouse Operations |
| POS | POS Terminal |
| HR edit, Obligations add, POS payment | Modal Workflow |
