# Production-Readiness Gaps

Date: 2026-05-11
Project: Full ERP System

## Executive Assessment

The application is not yet production-ready for paying companies handling real financial data. It has useful production preparation assets: Docker, PostgreSQL, role-based authorization, tenant isolation checks, audit logs, a production checklist, and tests. The blocking gaps are security hardening, operational controls, transaction/concurrency safety, accounting correctness, environment configuration, observability, backup/restore, and release automation.

## Critical Gaps

### 1. Seeded SuperAdmin Credentials

Current seed data creates `nasermustafi@gmail.com` with password `Admin123` if missing. This must be moved to environment-driven bootstrap or one-time setup before any hosted deployment.

Required:

- Read bootstrap email/password from secrets or environment.
- Force password change or one-time setup token.
- Disable hardcoded production credentials.
- Add a production startup guard that refuses known default credentials.

### 2. Weak Authentication Defaults

Current Identity password rules are relaxed and login uses `lockoutOnFailure: false`.

Required:

- Strong production password policy.
- Account lockout/rate limiting for failed login.
- Email confirmation for production users.
- Optional MFA for SuperAdmin/Admin/Accountant.
- Secure session lifetime and idle timeout policy.

### 3. HTTPS Is Disabled

`UseHttpsRedirection` is commented out. HTTPS is listed in docs as required but not enforced in runtime.

Required:

- Enable HTTPS redirection in production.
- Configure forwarded headers if behind reverse proxy.
- Secure cookies: `Secure`, `HttpOnly`, `SameSite`.
- HSTS only after proxy/domain setup is verified.

### 4. Runtime Database Migration On App Startup

The app calls `context.Database.Migrate()` during startup. That is risky for production deploys with multiple app instances or large migrations.

Required:

- Move migrations to a controlled deploy step.
- Add migration backup/rollback procedure.
- Add migration smoke tests.
- Keep startup migration only for local development if desired.

### 5. Missing Transaction Boundaries In Key Workflows

Some workflows use transactions, but POS sale creation, stock deduction, discount usage update, stock movements, customer stats, and audit are not wrapped in one durable transaction.

Required:

- Transactional POS sale service.
- Concurrency checks for stock and duplicate sale numbers/client IDs.
- Idempotency keys for offline and payment workflows.
- Rollback-safe audit strategy.

### 6. Accounting Is Incomplete For Production Finance

Journal posting exists but not all source documents post complete entries. Some reports are management estimates rather than accountant-grade financial statements.

Required:

- Define posting rules for every source document.
- Lock posted documents.
- Implement reversals, credit/debit notes, and void flows.
- Enforce fiscal period close/open.
- Add audit trail for posting/reversal.

### 7. Tenant Isolation Needs Full Query Coverage

`SaveChanges` validation is strong, but tenant isolation also depends on every query and controller path. Some service methods use `IgnoreQueryFilters` intentionally. A full security review is needed.

Required:

- Integration tests for every controller/module route across two companies.
- Central tenant-scoped query helpers or repository patterns for high-risk modules.
- Review all `IgnoreQueryFilters` use.
- Add automated tests that simulate cross-company IDs in form posts.

## High Gaps

### Secrets And Configuration

Current `.env.example` is helpful, but production secrets are not formalized.

Required:

- Document required production environment variables.
- Use a secret manager in hosted environments.
- Remove obsolete `POS_DB_PATH` fallback note if PostgreSQL is the only runtime database.
- Make platform owner email configurable.

### Observability

Logging is console/debug only. There are no health checks, correlation IDs, structured logs, metrics, or error tracking.

Required:

- Structured logging, ideally Serilog or equivalent.
- Request correlation ID.
- Health checks for app and PostgreSQL.
- Error tracking and alerting.
- Slow query and failed job logging.

### Backup And Restore

Docs mention backups, but no automated backup implementation exists.

Required:

- PostgreSQL scheduled backups.
- Backup retention policy.
- Restore drill procedure.
- Include uploaded images and attachments.
- Tenant export backup plan.

### CI/CD And Release Safety

No CI workflow was found in the repository inspection.

Required:

- Build/test pipeline.
- EF migration validation in CI.
- Docker image build.
- Dependency vulnerability scan.
- Release checklist and smoke tests.

### Production Docker Hardening

Docker Compose is suitable for local/dev, but production deployment needs hardening.

Required:

- Non-root container user.
- Explicit resource limits.
- Persistent storage backup strategy.
- Reverse proxy/TLS setup.
- Environment-specific compose or deployment manifests.

### File Upload Safety

Logo/product image upload exists. Attachment model exists. Production upload controls need hardening.

Required:

- File size limits.
- Content type validation and extension allowlist.
- Randomized filenames already appear likely, but confirm everywhere.
- Malware scanning if attachments are used for real customers.
- External object storage option.

### Audit Log Coverage And Integrity

Audit service supports before/after JSON but many actions log only summaries.

Required:

- Before/after logging for sensitive updates.
- Immutable audit retention policy.
- Export retention.
- Audit events for permission denied, login failure, posting, reversal, data export, and tenant admin actions.

## Medium Gaps

### Validation Layer

Validation is distributed across controllers and services.

Required:

- Central workflow validators for POS sale, returns, PO, goods receipt, supplier invoice, payments, payroll.
- Consistent business error responses.
- Prevent negative/invalid money values and invalid statuses across all modules.

### Permissions Model

Roles are coarse. ERP customers usually need fine-grained permissions.

Required:

- Permission catalog, e.g. `Sales.Post`, `Purchasing.Approve`, `Inventory.Adjust`, `Accounting.ClosePeriod`.
- Role-permission mapping per company.
- UI hides actions based on permissions.
- Controllers enforce permissions server-side.

### Background Jobs

Subscription monitor exists as hosted service, but broader scheduled work is missing.

Required:

- Daily backup job.
- Report snapshots.
- Subscription alerts.
- Low-stock notifications.
- Offline sync remediation.
- Cleanup jobs for temp files.

### Performance Readiness

Many indexes exist, but reporting queries will grow expensive.

Required:

- Add composite indexes for high-volume queries.
- Pagination everywhere, not only some pages.
- Report snapshots/materialized views for dashboards.
- Load testing with realistic sales, stock movements, audit logs, and journal entries.

### UI Production Polish

The UI is broad but still appears MVP/backoffice. Professional ERP polish remains.

Required:

- Consistent navigation by role/module group.
- Standard table filters and pagination.
- Empty/loading/error states.
- Responsive tables.
- Consistent PDF/print templates.

## Verification Gaps

Existing tests are valuable but incomplete for production readiness.

Needed test coverage:

- End-to-end POS sale with stock decrement, audit, customer stats, payment, and journal posting.
- Offline duplicate and retry behavior.
- Cross-company route/form post tests for every module.
- Approval blocking before PO receive/post.
- Period close blocking.
- Posted document edit prevention.
- Supplier invoice AP posting/payment.
- Returns/refunds accounting.
- Payroll duplicate period prevention and payslip generation.

Tests were not run during planning artifact creation to avoid changing build outputs; this was an inspection-only task.

## Production Readiness Exit Criteria

The system can be considered production-ready for a controlled pilot when:

- No default credentials exist.
- HTTPS, secure cookies, lockout, and production secrets are configured.
- Migrations run through deploy pipeline, not implicit production startup.
- Daily backups and restore drill are proven.
- Core financial workflows are transactional and idempotent.
- Tenant isolation route tests pass.
- Posted financial documents are locked and reversible.
- Logs, health checks, and alerts are active.
- CI runs build and test suite on every change.
- A deployment runbook exists.
