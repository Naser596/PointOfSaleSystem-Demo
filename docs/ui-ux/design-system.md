# ERP/SaaS Design System

Date: 2026-05-11
Scope: Existing ASP.NET Core MVC POS/ERP system
Guidance used: local `.cursor/skills/ui-ux-pro-max` plus current Razor/CSS inspection.

## Purpose

This design system defines a professional ERP/SaaS UI direction for the existing application. It must be applied incrementally to the current Razor views, Bootstrap/Bootswatch stack, Font Awesome icons, Chart.js charts, tenant branding variables, and existing business flows.

Do not rewrite the app. Do not change business logic while applying this system. Standardize the UI around the current structure.

## Current UI Foundation

The app already uses:

- ASP.NET Core MVC with Razor views in `Views/`.
- Bootstrap 5 with Bootswatch Zephyr.
- Inter font loaded globally.
- Font Awesome icons.
- Chart.js for dashboards.
- SweetAlert2 for confirmation/toast-style interaction.
- Tenant branding variables in `Views/Shared/_Layout.cshtml`: `--tenant-primary`, `--tenant-primary-soft`.
- Light/dark mode through `data-bs-theme`.
- Existing component patterns: `.page-header`, `.stat-card`, `.card`, `.table`, `.empty-state`, POS-specific layout classes, modal styles, global loading overlay.

Keep these foundations and refine them into one consistent enterprise design language.

## Design Direction

UI/UX Pro Max recommendation for this product type:

- Product type: B2B SaaS / ERP / POS dashboard.
- Style: flat, clean, professional, accessible, low decoration.
- Palette: navy/slate base with blue actions and restrained status colors.
- Typography: single professional sans family. Current `Inter` is acceptable and should remain unless a later redesign switches globally to `Plus Jakarta Sans`.
- Effects: minimal shadows, visible borders, 150-250ms transitions, no decorative gradients except controlled status cards.
- Avoid: marketing-style pages, excessive animation, oversized cards, one-off colors, emoji icons, layout-shifting hover effects.

## Color System

### Core Tokens

Use existing CSS custom properties as the source of truth and align new styles to them.

| Token | Current role | Recommended value/use |
| --- | --- | --- |
| `--bg-body` | App background | Very light slate, `#F8FAFC` style |
| `--bg-surface` | Cards/forms/tables | White in light mode, dark slate in dark mode |
| `--bg-surface-secondary` | Subtle headers/toolbars | Light slate tint; not pure gray noise |
| `--text-main` | Primary text | Slate/navy, high contrast |
| `--text-muted` | Secondary text | Minimum readable contrast; do not use pale gray for body |
| `--border-color` | Card/table/input borders | Visible in both themes |
| `--tenant-primary` | Brand/action primary | Tenant color, default should behave like professional blue |
| `--tenant-primary-soft` | Soft selected states | Use for selected filters, badges, active backgrounds |

### Recommended Semantic Colors

| Semantic | Use | Recommended direction |
| --- | --- | --- |
| Primary | Main action, selected state, links | Blue/navy via `--tenant-primary` |
| Success | Paid, synced, active, completed | Green, not used for general primary actions |
| Warning | Low stock, pending, variance | Amber/yellow with readable text |
| Danger | Overdue, failed, destructive | Red only for real risk/destructive states |
| Info | Neutral informational states | Cyan/blue sparingly |
| Secondary | Draft, inactive, neutral | Slate/gray |

### Rules

- Use color plus text/icon for status. Do not rely on color alone.
- Keep financial positive/negative colors consistent: positive green, negative red, warning amber.
- Avoid gradients for normal content cards. Current stat-card gradients can remain during transition, but future KPI cards should move toward flatter surfaces with colored accents.
- Tenant brand color should affect primary buttons, active navigation, selected filters, and key accents, not every component.

## Typography

Current `Inter` is a strong fit for ERP dashboards. Keep it.

### Type Scale

| Element | Target style |
| --- | --- |
| Page H1 | 1.75rem-2rem, 700-800, compact line-height |
| Section/card title | 1rem-1.125rem, 700 |
| Table header | 0.75rem-0.8rem, 700-800, uppercase, slight letter spacing |
| Body text | 0.95rem-1rem, 400-500 |
| Metadata/help text | 0.8125rem-0.875rem, muted |
| KPI value | 1.5rem-2rem; only larger for dashboard hero metrics |

### Rules

- Do not use hero-scale headings inside cards.
- Do not scale font size based on viewport width.
- Numeric financial values should align right in tables.
- Use tabular number styling later if introduced globally.
- Long names/descriptions must truncate or wrap predictably.

## Layout System

### Application Shell

Current shell: fixed top navbar with dropdowns.

Target shell:

- Desktop ERP backoffice should move toward a left sidebar + top utility bar.
- POS terminal may keep a specialized full-width operational layout.
- SuperAdmin can use a platform-oriented shell with companies/subscriptions/health navigation.
- Keep `container` width for now, but standardize page max-width and spacing.

### Spacing

Use Bootstrap spacing consistently:

- Page gap between major sections: `mb-4` or `mb-5`.
- Card body padding: `p-3` for dense tables/forms, `p-4` for document/detail pages.
- Grid gap: `g-3` for forms, `g-4` for dashboards.
- Avoid nested cards. Use cards for individual panels only.

### Page Header

Every page should start with `.page-header`:

- Left: icon, H1, one-line business description.
- Right: primary actions and secondary navigation.
- On mobile, actions wrap below title.

Header action order:

1. Primary create/post action.
2. Export/print/PDF.
3. Secondary back/navigation.

## Navigation

Current navigation is a top navbar with ERP and Admin dropdowns. It is workable but crowded.

Target desktop navigation groups:

- Dashboard
- POS
- Sales
  - Sales History
  - Sales Documents
  - Customers
  - Discounts
- Purchasing
  - Purchase Orders
  - Supplier Invoices
- Inventory
  - Products
  - Categories
  - Warehouses
- Finance
  - Finance
  - Accounting
  - Bank & Cash
  - Obligations
  - Reports
- People
  - Users
  - HR & Payroll
- Control
  - Approvals
  - Offline Sync
  - Audit Logs
- Platform, SuperAdmin only
  - Companies
  - New Company
  - Subscription/health views later

Rules:

- Show active route clearly.
- Keep role-based visibility.
- Use Font Awesome consistently until/unless the app switches icon set.
- Icon-only buttons need accessible labels.
- Mobile should collapse to an offcanvas or stacked navigation, not a giant dropdown list.

## Components

### Cards

Use cards for:

- KPI panels.
- Data tables.
- Forms.
- Detail summaries.
- Modals.

Avoid:

- Card inside card.
- Decorative cards for page sections.
- Excessive rounded corners. ERP should use 8-12px radii, not pill-shaped panels except badges/filter chips.

Recommended card anatomy:

- Header: icon + concise title + optional actions.
- Body: form/table/content.
- Footer: secondary actions or summary only.

### KPI Cards

Current `.stat-card` is visually strong but too dominant when repeated heavily, especially on Reports.

Target:

- 4-8 primary KPI cards per page maximum.
- Use compact metric cards for dense ERP reports.
- Each card has label, value, optional delta/status, optional icon.
- Do not use huge icons that compete with data.
- Prefer flat surface with left/top accent for future redesign.

Status mapping:

- Revenue/profit: primary/success.
- Payables/obligations: warning/danger if overdue.
- Inventory value: info/primary.
- Counts: secondary/info.

### Tables

ERP is table-heavy. Tables must be dense, readable, and consistent.

Standard table rules:

- Wrap every wide table in `.table-responsive`.
- Header background uses `--bg-surface-secondary`.
- Primary entity column first, with bold identifier and muted secondary text.
- Numeric money/quantity columns right aligned.
- Status columns use badges with text labels.
- Actions column last, right aligned.
- Use compact icon+text buttons when space allows; icon-only buttons must have `aria-label`.
- On mobile, either horizontal scroll or convert selected high-value tables to stacked row cards later.

Recommended column order:

1. Identifier / name.
2. Related party / category.
3. Dates.
4. Status.
5. Quantities or money.
6. Actions.

### Forms

Current forms are mostly inline Bootstrap grids. Keep the pattern but standardize.

Rules:

- Every input must have a visible `<label>`.
- Placeholder is hint text, not a label replacement.
- Use correct input types: date, number, email, tel.
- Use `autocomplete` where useful, especially login/company/user forms.
- Group related fields into sections for long forms.
- Primary submit button goes bottom-right or full-width only in small embedded forms.
- Required fields must be marked consistently.
- Inline validation/errors should appear near the field.

Form layout patterns:

- Short create form embedded in list page: card with 2-4 columns.
- Long create/edit form: dedicated page, not modal.
- High-risk action form: modal or dedicated confirmation page with explicit reason field.

### Filters And Search

Current filters exist on reports, obligations, products, POS, audit logs.

Standard filter bar:

- Card or toolbar directly below page header.
- Left: search box.
- Middle: date/status/category filters.
- Right: Apply/Reset/Export.
- Preserve filter state in URL query parameters.
- Use debounced search only when it does not surprise users.
- Show active filter chips below the toolbar for complex filters.
- Empty results must say whether filters caused the result.

### Modals

Use modals for:

- Confirming destructive actions.
- Short, reversible actions.
- Payment method selection.
- Quick edit only when fields are limited.

Avoid modals for:

- Long forms.
- Complex accounting/warehouse workflows.
- Multi-step document creation.

Modal rules:

- Header with icon + action title.
- Body starts with consequence/context.
- Footer has secondary Cancel on left/first and primary/destructive action on right/last.
- Close buttons need `aria-label`.
- Destructive actions require clear confirm copy.

### Invoices And Documents

Invoice, purchase order, supplier invoice, receipt, print/PDF pages should share one document visual system.

Document layout:

- Header: company identity left, document type/number/status right.
- Party block: customer/supplier, billing/shipping/matching.
- Metadata strip: date, due date, status, payment status, source document.
- Lines table: description, product/SKU, quantity, unit, tax, total.
- Totals panel: subtotal, discount, tax, paid, balance/total.
- Notes/footer.
- Action bar: Print, PDF, Back, Convert/Post/Pay where relevant.

Visual rules:

- Document pages should use white document surface even in dark mode if intended for print preview, or clearly separate screen view from print view.
- Use consistent status badges across sales/supplier documents.
- Avoid mixing `ToString("C")` and manual `$` formatting in the same UI long term; choose a currency display strategy.

### Inventory Pages

Inventory needs scan-friendly operational density.

Rules:

- Product tables need image/name/SKU/category/stock/status/actions.
- Stock status must show current and minimum values.
- Warehouse stock views should group by product or warehouse depending on task.
- Stock adjustment/transfer forms should visually warn when action changes inventory.
- Low stock rows should use icon + badge, not only row color.
- Future stock ledger should use a table-first layout with filter bar.

### Accounting And Reports

Accounting pages should be quieter and more data-dense than POS pages.

Rules:

- Use right-aligned money columns.
- Trial balance, P&L, balance sheet need totals rows that are visually distinct but not loud.
- Posted/draft/closed statuses should be clear.
- Report pages should not show too many large KPI cards. Use grouped summaries plus drilldown tables.
- Charts should support the table, not replace it.

Recommended charts:

- Line chart for trend over time.
- Bar chart for category/user/product comparison.
- Doughnut only for small part-to-whole sets like payment split.
- Always include labels/tooltips and readable legends.

### Loading States

Current global overlay exists and product/POS AJAX uses opacity.

Rules:

- Use global overlay only for blocking whole-page operations.
- Use local skeleton/spinner for table/filter updates.
- For AJAX tables, show "Loading products..." or "Loading records..." with `aria-live="polite"`.
- Buttons that submit should show spinner/progress and prevent accidental double submit.
- Loading must not shift layout.

### Empty States

Current `.empty-state` exists. Standardize it.

Empty state anatomy:

- Icon.
- Clear title.
- One sentence explaining why it is empty.
- One primary action if the user can fix it.

Examples:

- Products: "No products found" + "Add Product" or "Clear filters".
- Accounting: "No journal entries yet" + "Post a document or create opening balance".
- Warehouse: "No warehouse stock yet" + "Post goods receipt".
- Reports: "No data for this period" + "Change date range".

### Error States

Rules:

- Use inline errors for forms.
- Use alert/card errors for page-level failures.
- Use toasts for transient success/error after actions.
- Give recovery steps: retry, clear filters, go back, contact platform owner.
- Do not expose raw exception text to end users.

### Responsive Behavior

Breakpoints to test:

- 375px mobile.
- 768px tablet.
- 1024px small desktop.
- 1440px desktop.

Rules:

- Tables use horizontal scroll at minimum.
- Touch targets at least 44px on mobile.
- Page header actions wrap below title.
- POS cart moves below product grid on tablet/mobile, which current CSS already does.
- Fixed/sticky elements must not overlap the online badge, navbar, or mobile browser chrome.
- Avoid horizontal page scroll except inside intentional table wrappers.

## Accessibility Rules

From UI/UX Pro Max web guidance:

- Icon-only buttons need accessible names.
- Every form control needs a label or `aria-label`.
- Use semantic `<button>`, `<a>`, `<label>`, `<nav>`, `<main>`.
- Visible focus states are required.
- Async updates need `aria-live="polite"` where relevant.
- Decorative icons should use `aria-hidden="true"` in future cleanup.
- Destructive actions need confirmation.
- Color is never the only indicator.

## Implementation Guidance

When implementation starts:

1. Add/standardize shared CSS utilities in `wwwroot/css/site.css`.
2. Keep Bootstrap classes but create app-specific wrappers for repeat patterns.
3. Refactor layout/navigation first, then page templates, then individual pages.
4. Do not alter controller/service behavior while restyling.
5. Verify dark mode after each page group.
6. Use screenshots at desktop and mobile before marking UI work complete.
