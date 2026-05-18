# UI/UX Redesign Sprint: Phases 4-6

Date: 2026-05-11
Scope: UI-only continuation for the existing .NET POS/ERP system.

## Guardrails

- Do not rewrite the app.
- Do not change business logic, controller actions, route names, authorization, models, services, database, or form field names.
- Reuse the existing Razor MVC, Bootstrap, Bootswatch, Font Awesome, Chart.js, tenant branding, and dark mode foundation.
- Add reusable CSS utilities before page-specific redesign.

## Phase 4: Tables, Filters, And Search

Story: As an ERP user, I need list pages to use a consistent filter/search/table pattern so I can scan operational data quickly.

Tasks:

- Introduce a reusable filter/search toolbar pattern.
- Standardize table cards, action columns, money alignment, and no-results states.
- Preserve existing search/filter form names and AJAX behavior.
- Improve icon-only action labels.

Initial target pages:

- `Views/Products/Index.cshtml`
- `Views/Products/_ProductTable.cshtml`
- `Views/Customers/Index.cshtml`
- `Views/SalesDocuments/Index.cshtml`
- `Views/Purchasing/Index.cshtml`
- `Views/SupplierInvoices/Index.cshtml`

## Phase 5: Forms And Modals

Story: As an operator/admin, I need product and document-entry forms to feel predictable, grouped, and safer for data entry.

Tasks:

- Introduce reusable form shell, section, helper, and action bar styles.
- Use visible labels and field grouping.
- Keep required fields explicit.
- Preserve current form posts and validation helpers.

Initial target pages:

- `Views/Products/Create.cshtml`
- `Views/Products/Edit.cshtml`
- Inline create forms on Sales Documents and Purchasing.

## Phase 6: Documents And Invoice Pages

Story: As a business user, I need sales, purchasing, and supplier invoice pages to look like one professional document system.

Tasks:

- Introduce reusable document shell, toolbar, paper, metadata, party block, line table, and totals styles.
- Standardize print/PDF/back action bar.
- Keep document workflows and form posts intact.

Initial target pages:

- `Views/SalesDocuments/Details.cshtml`
- `Views/Purchasing/Details.cshtml`
- `Views/SupplierInvoices/Details.cshtml`

## Acceptance

- Existing links and form posts still work.
- Existing AJAX product filtering still works.
- Tables are wrapped and responsive.
- Empty states provide text and a next action where appropriate.
- Product create/edit still support image preview and barcode scanning.
- Sales/purchase/supplier invoice action buttons remain available.
- `dotnet build` passes.

## Out Of Scope

- Inventory and warehouse deep UX.
- Accounting and reports deep redesign.
- POS terminal polish.
- Full accessibility audit across every page.
