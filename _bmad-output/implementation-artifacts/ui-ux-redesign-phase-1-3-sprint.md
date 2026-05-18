# UI/UX Redesign Sprint: Phases 1-3

Date: 2026-05-11
Scope: UI-only implementation for the existing .NET POS/ERP system.

## Guardrails

- Do not rewrite the app.
- Do not change business logic.
- Do not change controllers, services, models, database, or authorization behavior for this sprint.
- Keep existing Razor MVC, Bootstrap, Bootswatch, Font Awesome, Chart.js, tenant branding, and dark mode.
- Keep POS operational flow unchanged.

## Phase 1: Shell And Navigation

Story: As an ERP user, I need a stable application shell with grouped navigation so I can move between modules without a crowded top bar.

Tasks:

- Add a desktop ERP sidebar while preserving the existing top navbar for brand, mobile navigation, user actions, and theme toggle.
- Group current links into Dashboard, POS, Sales, Inventory, Finance, Operations, Control, Admin, and Platform.
- Preserve all role checks and route URLs.
- Make main content respect fixed header and sidebar spacing.

Acceptance:

- All existing links are still present.
- SuperAdmin sees platform navigation.
- Admin and ERP roles see their existing module links.
- Mobile still uses the Bootstrap collapsed navbar.
- Desktop content does not hide behind fixed header/sidebar.

## Phase 2: Core Component CSS

Story: As a product team, we need consistent primitives so future pages can be redesigned without page-specific visual drift.

Tasks:

- Add ERP shell classes in `wwwroot/css/site.css`.
- Add flat dashboard KPI, section, action, chart, alert, and table utility classes.
- Improve focus states and responsive behavior.
- Keep existing POS-specific CSS intact.

Acceptance:

- Existing cards, forms, tables, modals, and POS classes still render.
- Dark mode contrast remains readable.
- New dashboard classes follow the documented design system.
- Hover/focus states do not shift layout.

## Phase 3: Main Dashboard

Story: As a manager/admin, I need the dashboard to show the most important business state first, with alerts, KPIs, trends, work queues, and recent activity.

Tasks:

- Reorganize `Views/Home/Index.cshtml` into clear dashboard zones.
- Limit the first KPI row to primary metrics.
- Keep existing chart IDs and data bindings.
- Keep existing quick actions and recent sales behavior.
- Keep obligation and low-stock alerts intact.

Acceptance:

- No more than four primary KPI cards appear at the top.
- Alerts appear before secondary content.
- Sales chart and payment chart still initialize.
- Recent sales empty state still links to POS.
- Non-admin users can still reach POS and sales history.

## Out Of Scope

- Data-list page redesigns.
- Forms and modal redesigns.
- Invoice/document redesigns.
- Accounting/report deep redesign.
- POS terminal redesign.
- Any business logic or data model changes.
