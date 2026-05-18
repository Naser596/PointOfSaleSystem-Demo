# UI/UX Pro Max And BMAD Bridge

Date: 2026-05-11
Project: Full ERP System

## Purpose

This bridge connects BMAD planning with the installed UI/UX Pro Max skill so future redesign work can move in a controlled sequence.

BMAD remains the planning and delivery source of truth:

- Roadmaps, epics, stories, sprint scope, and acceptance criteria live under `_bmad-output`.
- Project knowledge and durable UI guidance live under `docs`.
- Implementation artifacts live under `_bmad-output/implementation-artifacts`.

UI/UX Pro Max remains the visual quality source of truth:

- Installed path: `.cursor/skills/ui-ux-pro-max`
- Primary skill file: `.cursor/skills/ui-ux-pro-max/SKILL.md`
- Search script: `.cursor/skills/ui-ux-pro-max/scripts/search.py`
- Current ERP/SaaS recommendation: professional B2B SaaS, flat design, clean navy/slate palette, blue CTA, strong accessibility, minimal animation.

## Design Inputs

Use these documents before changing UI code:

- `docs/ui-ux/design-system.md`
- `docs/ui-ux/page-templates.md`
- `docs/ui-ux/redesign-roadmap.md`

Use UI/UX Pro Max searches when a page type is unclear:

```powershell
python .cursor\skills\ui-ux-pro-max\scripts\search.py "B2B SaaS ERP POS accounting dashboard professional clean" --design-system -f markdown -p "Full ERP System"
python .cursor\skills\ui-ux-pro-max\scripts\search.py "dashboard responsive tables forms loading empty error states" --domain ux -n 8
```

## Delivery Contract

Every BMAD UI story must reference:

- The page template used from `docs/ui-ux/page-templates.md`.
- The design tokens and component rules from `docs/ui-ux/design-system.md`.
- The current phase in `docs/ui-ux/redesign-roadmap.md`.
- A short UI/UX Pro Max note if the story introduces a new pattern.

Every UI implementation must preserve:

- Existing controllers, services, routes, authorization checks, and form posts unless a separate business story explicitly changes them.
- Bootstrap, Bootswatch, Font Awesome, Chart.js, Razor MVC, tenant branding, and existing dark mode support.
- POS operational speed and current workflow behavior.

## Phase Limit For First Redesign Sprint

The first implementation sprint is intentionally limited to three phases:

1. Phase 1: Shell and navigation standardization.
2. Phase 2: Core component CSS cleanup.
3. Phase 3: Main dashboard redesign.

Phases 4-10 remain planned but out of scope for this sprint.

## Acceptance Checklist

- All current links remain reachable.
- Role checks remain intact.
- No controller or service business logic changes.
- Light and dark themes remain readable.
- Desktop layout works at 1024px and 1440px.
- Mobile layout works at 375px and 768px.
- Tables stay inside responsive wrappers.
- Empty states provide message and action.
- Loading states remain visible.
- Error/alert states include clear recovery action.
- Interactive controls have visible hover and focus states.
- Icons continue to use Font Awesome.

## Communication Rule

When continuing UI work, start from the BMAD story, then validate the visual decision against UI/UX Pro Max. If they disagree, prefer the existing app structure and business workflow, then document the visual compromise in the implementation artifact.
