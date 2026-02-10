# JobTracker — TODO

## High Priority
- [x] **Invoice Line Items UI** — DB and API already support line items; build UI in InvoiceFormDialog to add/edit/remove itemized rows (description, quantity, unit price). Required before invoice template auto-fill is useful.
- [x] **Overdue Auto-Detection** — Invoices past their due date should automatically flip to "Overdue." Either scan on dashboard load or run a scheduled background check.
- [ ] **Expense / Cost Tracking** — Log costs against a job (materials, subcontractors, software, etc.) to calculate profit, not just revenue. Needs DB table, API endpoints, and job detail UI.

## High Priority (New Feature)
- [x] **Time Clock Tab** — New top-level tab with a punch-in/punch-out system. Job codes to associate punches with specific jobs, passcode entry for authentication, and time tracking visible on the dashboard. Needs DB tables (TimePunch, job code associations), API endpoints, dedicated UI tab, and dashboard integration.

## Medium Priority
- [ ] **Dashboard Revenue Period Filter** — Summary cards are currently all-time. Add month/quarter/year filtering so the numbers reflect a meaningful time window.
- [ ] **Global Search** — Quick-find across clients, jobs, and invoices by name or number. Search bar in the nav or header with results dropdown.
- [ ] **Job Detail View** — Match the client detail pattern: double-click a job to see linked invoices, expenses, and status history in a tabbed detail page.

## Lower Priority
- [x] **Time Tracking** — For hourly billing jobs, allow logging hours with date, description, and rate. Ties into invoicing and profitability.
- [ ] **Reporting / CSV Export** — Pull data out for tax season or bookkeeping. Revenue by client, revenue by period, outstanding invoices, expense summaries.
- [ ] **Invoice Template Auto-Fill** — Populate the .docx template with client name, address, invoice number, line items, amounts, and dates on creation.
