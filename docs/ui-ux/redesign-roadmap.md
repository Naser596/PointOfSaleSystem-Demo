# UI/UX Redesign Roadmap

Date: 2026-05-11
Scope: Existing .NET POS/ERP app, approximately 70% complete.

## Goal

Make the ERP look modern, clean, professional, and consistent without breaking existing functionality. The redesign must be incremental, preserve current business logic, and reuse the existing Razor/Bootstrap project structure.

## Non-Goals

- No rewrite.
- No change to business logic during UI-only phases.
- No migration to React/Vue/Tailwind as part of this roadmap.
- No removal of existing features.
- No redesign that hides operational workflows behind marketing-style visuals.

## Current UI Strengths

- Solid Bootstrap 5 foundation.
- Good use of cards, stat cards, tables, modals, and responsive grid.
- Tenant branding support already exists.
- Dark mode support exists.
- POS terminal has a purpose-built two-column layout.
- Reports/accounting/inventory modules already share many component patterns.
- Font Awesome provides consistent icon coverage.

## Current UI Risks

- Top navigation is crowded as ERP modules grow.
- Some pages use different card header styles and one-off `bg-white`, `bg-primary`, `shadow-sm` patterns.
- Reports page has too many large KPI cards, reducing scan efficiency.
- Some icon-only buttons lack accessible labels.
- Some forms rely on placeholder-only hints in dense layouts.
- Table actions and status badges are not fully standardized.
- Loading states are inconsistent: global overlay, opacity fade, blank areas.
- Empty states vary between `.empty-state` and ad hoc centered text.
- Invoice/document pages are visually close but not yet one system.
- Mobile table behavior is mostly horizontal scroll; some pages need better mobile task flows.

## Phase 0: Documentation And Alignment

Status: this phase creates the design guidance.

Deliverables:

- `docs/ui-ux/design-system.md`
- `docs/ui-ux/page-templates.md`
- `docs/ui-ux/redesign-roadmap.md`

Exit criteria:

- Future UI work can reference one design system.
- Page templates are mapped to current Razor views.
- Redesign work can be planned without changing business logic.

## Phase 1: Shell And Navigation Standardization

Goal: Make the application feel like an ERP/SaaS product before touching individual modules deeply.

Work:

- Refactor `_Layout.cshtml` visually into an ERP shell.
- Introduce left sidebar on desktop with grouped modules.
- Keep top utility bar for tenant logo/name, search later, online status, theme, user/logout.
- Convert current crowded top dropdowns into stable module groups.
- Keep mobile navigation as collapse/offcanvas.
- Add active nav state based on current route.

Suggested module groups:

- Dashboard
- POS
- Sales
- Purchasing
- Inventory
- Finance
- People
- Control
- Platform for SuperAdmin

Constraints:

- Preserve all links and role checks.
- Do not change authorization or controller routing.

Acceptance criteria:

- Users can reach every current page.
- Active section is visible.
- Mobile navigation works without horizontal scroll.
- No content hidden behind fixed header/sidebar.

## Phase 2: Core Component CSS Cleanup

Goal: Normalize visual primitives across existing pages.

Work:

- Standardize `.page-header`.
- Standardize cards and card headers.
- Standardize `.stat-card` and introduce a flatter compact KPI variant.
- Standardize badges for document/payment/sync/status states.
- Standardize `.empty-state`, local loading, and error panels.
- Add clear focus states for buttons, links, inputs, table actions.
- Add CSS for filter toolbar and action toolbar.
- Review dark mode contrast.

Constraints:

- Keep Bootstrap/Bootswatch classes.
- Add app-specific classes in `wwwroot/css/site.css`.
- Avoid breaking POS-specific classes.

Acceptance criteria:

- Dashboard, Products, SalesDocuments, Accounting, Reports, Warehouses still render correctly.
- Light and dark mode remain readable.
- Tables, cards, buttons, badges feel consistent.

## Phase 3: Dashboard Redesign

Pages:

- `Views/Home/Index.cshtml`
- `Views/Erp/Index.cshtml`
- `Views/SuperAdmin/Index.cshtml`

Goal: Improve scanning, priority, and professional SaaS feel.

Work:

- Convert dashboard into clear zones:
  - Command center / alerts.
  - Primary KPIs.
  - Trends.
  - Work queues.
  - Recent activity.
- Reduce oversized stat card overload.
- Use compact KPI cards for secondary metrics.
- Move quick actions below operational alerts.
- Standardize chart card heights and legends.

Acceptance criteria:

- First viewport shows the most important business state.
- No more than 4 primary KPI cards at top.
- Alerts are visually prioritized.
- Desktop/tablet/mobile layouts are coherent.

## Phase 4: Tables, Filters, And Search

Pages:

- Products
- Customers
- Sales
- SalesDocuments
- Purchasing
- SupplierInvoices
- AuditLogs
- Obligations
- OfflineSync

Goal: Make all data-list pages consistent and scalable.

Work:

- Create standard filter toolbar pattern.
- Standardize search input with icon and debounce behavior where needed.
- Standardize table action columns.
- Add consistent no-results empty state.
- Add local loading state for AJAX table refresh.
- Add pagination pattern where needed.
- Ensure all wide tables are inside `.table-responsive`.

Acceptance criteria:

- List pages use the same visual grammar.
- Search/filter areas are easy to find.
- Empty results distinguish "no data" from "filtered out".
- Icon-only actions have accessible names.

## Phase 5: Forms And Modals

Pages:

- Products Create/Edit
- Customers Create/Edit
- Users Create/Edit
- Settings
- SuperAdmin Create/Edit Company
- HR
- Obligations modal
- POS payment modal

Goal: Make data entry predictable and professional.

Work:

- Standardize form sections, labels, required indicators, helper text.
- Replace placeholder-only meaning with visible labels.
- Normalize submit/cancel button placement.
- Keep long forms as pages, not modals.
- Keep modals for short actions only.
- Standardize destructive confirmation language.

Acceptance criteria:

- All inputs have labels.
- Required fields are clear.
- Validation and TempData feedback are visually consistent.
- Modal actions are easy to understand and keyboard-accessible.

## Phase 6: Documents And Invoice Pages

Pages:

- SalesDocuments Details/Print
- Purchasing Details/Print
- SupplierInvoices Details/Print
- Sales Receipt

Goal: Make all ERP documents look like one professional document system.

Work:

- Create shared document visual rules.
- Standardize header, party blocks, metadata, line tables, totals, notes.
- Standardize action bar: Print, PDF, Back, Convert/Post/Pay.
- Improve print readability.
- Align status/payment badges.
- Normalize currency formatting display.

Acceptance criteria:

- Sales invoice, purchase order, supplier invoice, and receipt look related.
- Print/PDF pages are clean and readable.
- Totals and balance due are easy to scan.
- Mobile screen view remains usable.

## Phase 7: Inventory And Warehouse UX

Pages:

- Products
- Warehouses
- Purchasing goods receipt
- Reports inventory valuation

Goal: Make inventory operations safer and easier to scan.

Work:

- Improve product table density and stock status.
- Convert category sidebar to responsive filter chips/collapsible filter on mobile.
- Separate warehouse setup from stock operations visually.
- Make transfer/adjustment forms clearly operational and risk-aware.
- Add consistent stock badges: OK, Low, Out, Reserved.
- Future: stock ledger template.

Acceptance criteria:

- Users can quickly find low/out-of-stock items.
- Inventory-changing forms are visually distinct from setup forms.
- Warehouse stock table works on mobile through scroll or stacked view.

## Phase 8: Accounting And Reports UX

Pages:

- Finance
- Accounting
- Reports
- FinancialAccounts
- Obligations

Goal: Make finance screens credible for accountants and managers.

Work:

- Reduce KPI overload on Reports.
- Group reports by Finance, AR/AP, Inventory, Purchasing, Customers.
- Standardize totals rows and money alignment.
- Add report section headers and filter summary.
- Improve chart consistency.
- Use line charts for trends, bar charts for comparisons, doughnut only for small splits.

Acceptance criteria:

- Accounting pages feel dense and trustworthy, not decorative.
- Financial values align and totals are clear.
- Reports are readable at desktop and tablet sizes.
- Date range context is always visible.

## Phase 9: POS Polish

Pages:

- POS Terminal
- Card Terminal
- Receipt

Goal: Keep POS fast while making it cleaner and more touch-friendly.

Work:

- Keep current product/search/cart structure.
- Improve product card consistency and disabled/out-of-stock state.
- Standardize payment modal buttons.
- Make offline banner more actionable.
- Add local loading text for product refresh.
- Review keyboard shortcuts and visible hints.
- Ensure mobile cart checkout remains reachable.

Acceptance criteria:

- Search, product selection, cart, discount, payment remain fast.
- Touch targets are large enough.
- Offline state and pending sync are obvious.
- No layout shift during product filtering.

## Phase 10: Accessibility And Responsive Audit

Goal: Verify the professional UI works for real users and devices.

Work:

- Check keyboard navigation.
- Add visible focus states where missing.
- Add accessible labels to icon-only buttons.
- Check color contrast in light/dark mode.
- Test mobile widths: 375, 768, 1024, 1440.
- Ensure no horizontal page scroll outside table wrappers.
- Ensure charts have readable legends and tables as backup where needed.

Acceptance criteria:

- Core workflows are keyboard usable.
- Mobile layouts do not overlap.
- Dark mode remains readable.
- Critical controls meet touch target expectations.

## Recommended Implementation Order

1. Add design tokens/component utilities in CSS.
2. Update app shell/navigation.
3. Update page header/card/table/filter/empty state patterns.
4. Redesign dashboard and ERP core.
5. Redesign product/customer/sales document/purchasing list pages.
6. Redesign document detail/print pages.
7. Redesign accounting/reports pages.
8. Polish POS last, carefully, because it is operationally sensitive.

## Page Priority

Highest impact:

- `_Layout.cshtml`
- `Home/Index`
- `POS/Index`
- `Products/Index`
- `SalesDocuments/Index` and `Details`
- `Purchasing/Index` and `Details`
- `Reports/Index`
- `Accounting/Index`
- `Warehouses/Index`
- `SuperAdmin/Index`

Medium impact:

- `Finance/Index`
- `FinancialAccounts/Index`
- `Obligations/Index`
- `SupplierInvoices/*`
- `Customers/*`
- `Users/*`
- `Hr/Index`
- `AuditLogs/Index`
- `OfflineSync/Index`

## Definition Of Done For UI Stories

Every UI redesign story should verify:

- Existing actions and form posts still work.
- No controller/service/business logic changed unless explicitly part of a separate story.
- Light mode and dark mode checked.
- Desktop and mobile widths checked.
- Tables do not break layout.
- Empty/loading/error states exist where relevant.
- Icons are from the existing Font Awesome set.
- Accessibility basics: labels, focus states, button names.

## Notes For Future Development

- A design system is only useful if new pages follow it. New ERP modules should start from `page-templates.md`.
- When a new module is added, first choose a template, then implement the page.
- Avoid introducing a second UI framework until the existing Bootstrap/Razor system is consistent.
- If the app later adopts a component system, extract repeated Razor partials for page header, filter toolbar, KPI card, status badge, table empty state, and document totals.
