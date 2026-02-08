# TylerBuilds JobTracker — CRUD Implementation Plan

## Context

You are continuing work on the TylerBuilds JobTracker WPF desktop application. The Client CRUD system is fully working (create, edit, deactivate) and serves as the reference pattern for implementing Jobs and Invoices.

**Read these files first to understand the established patterns:**
- `project_ref.md` (root of frontend project) — full architecture reference
- `db_ref.md` (root of API project) — all stored procedures and their params
- `Services/ApiClient.cs` — HTTP client (uses camelCase JSON serialization)
- `Services/AppConfig.cs` — config loader
- `Models/CreateClientRequest.cs` and `Models/UpdateClientRequest.cs` — request DTOs
- `ViewModels/ClientFormViewModel.cs` — form ViewModel pattern (handles both create and edit)
- `ViewModels/ClientsViewModel.cs` — list ViewModel pattern (load, new, edit, deactivate commands)
- `Views/ClientFormDialog.xaml` — form dialog pattern (MaterialDesign outlined inputs, save/cancel)
- `Views/ClientsView.xaml` — list view pattern (header with action buttons, DataGrid, double-click edit)
- `Views/ClientsView.xaml.cs` — code-behind pattern (OnLoaded, OnRowDoubleClick)
- `App.xaml` — registered converters
- `App.xaml.cs` — composition root (service wiring, navigation registration)
- `Converters/` — all existing converters

**Critical conventions:**
- C# PascalCase properties on models, but ApiClient serializes as camelCase to match Python backend
- MaterialDesign styles: OutlinedTextBox, OutlinedComboBox, RaisedButton, OutlinedButton, MaterialDesignDataGrid
- Form dialogs are modal Windows opened via ShowDialog(), not UserControls
- Form ViewModels take `System.Windows.Window` as command parameter for closing the dialog
- Commands use CommunityToolkit.Mvvm `[RelayCommand]` attribute generation
- List ViewModels own the dialog lifecycle (create VM, create dialog, set DataContext, ShowDialog, refresh on success)
- The `NotNullToBoolConverter` is used for enabling Edit/Deactivate buttons based on selection
- All list views auto-load data via `Loaded="OnLoaded"` in XAML

---

## Phase 1: Job CRUD

### 1A. Request Models

Create `Models/CreateJobRequest.cs`:
```
Properties: ClientId (int), PrimaryContactId (int?), Title (string), Description (string?), 
Status (string, default "Lead"), BillingType (string?), HourlyRate (decimal?), 
FixedPrice (decimal?), RetainerAmount (decimal?), EstimatedValue (decimal?), 
StartDate (DateTime?), EndDate (DateTime?), Notes (string?)
```

Create `Models/UpdateJobRequest.cs`:
```
Same fields as create, minus Status (status changes go through a separate endpoint)
```

Create `Models/UpdateJobStatusRequest.cs`:
```
Properties: Status (string)
```

### 1B. JobFormViewModel

Create `ViewModels/JobFormViewModel.cs` following the ClientFormViewModel pattern:
- Constructor overloads: one for create (takes ApiClient), one for edit (takes ApiClient + JobModel)
- Needs to load clients list and contacts list for dropdowns
  - On construction (create mode): call `GET /clients?active_only=true` to populate a `List<ClientModel>` for the Client dropdown
  - When SelectedClient changes: call `GET /contacts/by-client/{clientId}` to populate contacts dropdown
  - On construction (edit mode): pre-populate all fields from the JobModel, load clients, then set SelectedClient which triggers contact load
- Expose `List<string> Statuses` for create mode: `["Lead", "Proposal", "Active", "Completed", "Invoiced"]`
- Expose `List<string> BillingTypes`: `["Hourly", "Fixed", "Retainer"]`
- SaveCommand: POST /jobs (create) or PUT /jobs/{id} (edit)
- CancelCommand: close dialog
- Include a `LoadClientsCommand` that runs on dialog Loaded event

**Important:** Money fields (HourlyRate, FixedPrice, RetainerAmount, EstimatedValue) should be `decimal?` on the ViewModel and nullable in the request models. Empty textbox = null, not 0.

### 1C. JobFormDialog

Create `Views/JobFormDialog.xaml` and `.xaml.cs`:

Layout (top to bottom):
1. Client dropdown (ComboBox bound to Clients list, DisplayMemberPath="Name", SelectedItem bound to SelectedClient)
2. Contact dropdown (ComboBox bound to Contacts list, display first+last name, enabled only when client is selected)
3. Title textbox (required)
4. Description textbox (multiline)
5. Status dropdown (only shown in create mode — use a `Visibility` binding on `IsEdit` with InverseBoolToVisConverter)
6. Billing Type dropdown
7. Money fields row: HourlyRate / FixedPrice / RetainerAmount side by side (show/hide based on BillingType selection, or just show all three)
8. Estimated Value textbox
9. Start Date / End Date row (use MaterialDesign DatePicker: `Style="{StaticResource MaterialDesignOutlinedDatePicker}"`)
10. Notes textbox (multiline)
11. Error message TextBlock
12. Cancel / Save buttons

Dialog should be wider than client dialog — `Width="600"`.

### 1D. Update JobsViewModel

Update `ViewModels/JobsViewModel.cs`:
- Add `NewJobCommand` → creates JobFormViewModel, opens JobFormDialog, refreshes on save
- Add `EditJobCommand` → fetches fresh job via GET /jobs/{id}, creates edit-mode VM, opens dialog
- Add `UpdateStatusCommand` → could be a right-click context menu or a small dropdown, calls PATCH /jobs/{id}/status
- Keep existing `LoadDataAsync` and `StatusFilter`

### 1E. Update JobsView

Update `Views/JobsView.xaml`:
- Wire "New Job" button to `NewJobCommand`
- Add "Edit" button bound to `EditJobCommand`, enabled via `NotNullToBoolConverter` on `SelectedJob`
- Add double-click handler on DataGrid (same pattern as ClientsView)
- Status filter ComboBox should trigger reload: add `SelectionChanged` handler or use property changed callback

Update `Views/JobsView.xaml.cs`:
- Add `OnRowDoubleClick` handler

### 1F. Validation

Before saving, validate:
- Title is required
- ClientId is required (a client must be selected)

---

## Phase 2: Invoice CRUD

### 2A. Request Models

Create `Models/CreateInvoiceRequest.cs`:
```
Properties: JobId (int), Amount (decimal), IssuedDate (DateTime?), DueDate (DateTime?), Notes (string?)
```
Note: invoiceNumber and iteration are auto-generated by the stored procedure.

Create `Models/UpdateInvoiceRequest.cs`:
```
Properties: Amount (decimal), IssuedDate (DateTime?), DueDate (DateTime?), Notes (string?)
```

Create `Models/UpdateInvoiceStatusRequest.cs`:
```
Properties: Status (string)
```

Create `Models/CreateLineItemRequest.cs`:
```
Properties: InvoiceId (int), Description (string), Quantity (decimal), UnitPrice (decimal)
```

Create `Models/UpdateLineItemRequest.cs`:
```
Properties: Description (string), Quantity (decimal), UnitPrice (decimal)
```

### 2B. InvoiceFormViewModel

Create `ViewModels/InvoiceFormViewModel.cs`:
- Constructor overloads: create (takes ApiClient) and edit (takes ApiClient + InvoiceModel)
- Needs to load jobs list for dropdown: `GET /jobs` — display Title + ClientName
- Expose `List<string> Statuses`: `["Draft", "Sent", "Paid", "Overdue", "Cancelled"]`
- In create mode: status is auto-set to Draft, no status field needed
- In edit mode: show status field for updating
- SaveCommand: POST /invoices (create) or PUT /invoices/{id} (edit)
- Status update is separate: either a button on the edit form or handled from the list view

**Line Items sub-section (edit mode only):**
- When editing an invoice, load line items via `GET /invoices/{id}/line-items`
- Display in a small DataGrid within the form
- "Add Line Item" button → inline row or small sub-dialog
- Each line item: Description, Quantity, UnitPrice, LineTotal (computed, read-only)
- Save line items via POST /invoices/line-items (create) or PUT /invoices/line-items/{id} (update)
- After any line item save, the stored procedure auto-recalculates the invoice total — refresh the Amount display

**Alternative simpler approach:** Skip inline line item editing in the form dialog. Instead, just have the basic invoice fields in the form. Line item management can be a separate detail view later. This is recommended for first pass — get the core invoice CRUD working first, add line items as a follow-up.

### 2C. InvoiceFormDialog

Create `Views/InvoiceFormDialog.xaml` and `.xaml.cs`:

Layout (top to bottom):
1. Job dropdown (ComboBox bound to Jobs list, display "Title — ClientName")
2. Amount textbox (decimal input)
3. Issued Date (DatePicker)
4. Due Date (DatePicker)
5. Status dropdown (edit mode only)
6. Notes textbox (multiline)
7. Error message TextBlock
8. Cancel / Save buttons

Width="500".

### 2D. Update InvoicesViewModel

Update `ViewModels/InvoicesViewModel.cs`:
- Add `NewInvoiceCommand` → opens InvoiceFormDialog in create mode
- Add `EditInvoiceCommand` → fetches fresh invoice, opens edit-mode dialog
- Add `UpdateStatusCommand` → quick status change (e.g., mark as Sent, mark as Paid)
- Keep existing load and filter

### 2E. Update InvoicesView

Update `Views/InvoicesView.xaml`:
- Wire "New Invoice" button to `NewInvoiceCommand`
- Add "Edit" button, enabled when selected
- Add double-click handler
- Optional: Add quick-action buttons like "Mark Paid" that call UpdateStatusCommand

Update `Views/InvoicesView.xaml.cs`:
- Add `OnRowDoubleClick` handler

### 2F. Validation

Before saving:
- JobId is required
- Amount must be > 0

---

## General Implementation Notes

### API Endpoints Reference (from db_ref.md)
- `POST /clients` — create client
- `PUT /clients/{id}` — update client  
- `PATCH /clients/{id}/deactivate` — soft delete
- `GET /contacts/by-client/{id}` — contacts for a client
- `POST /jobs` — create job
- `PUT /jobs/{id}` — update job
- `PATCH /jobs/{id}/status` — update status only (body: `{"status": "Active"}`)
- `GET /jobs` — all jobs, optional `?status=` filter
- `GET /jobs/{id}` — single job with enriched data
- `GET /jobs/by-client/{id}` — jobs for a client
- `POST /invoices` — create invoice (auto-generates number + iteration)
- `PUT /invoices/{id}` — update invoice
- `PATCH /invoices/{id}/status` — update status (body: `{"status": "Paid"}`, auto-sets paidDate)
- `GET /invoices` — all invoices, optional `?status=` filter
- `GET /invoices/{id}` — single invoice with job/client data
- `GET /invoices/{id}/line-items` — line items for invoice
- `POST /invoices/line-items` — create line item (auto-recalculates invoice total)
- `PUT /invoices/line-items/{id}` — update line item

### Pattern Checklist (for each entity)
- [ ] Request model(s) in `Models/`
- [ ] Form ViewModel in `ViewModels/` with create + edit constructors
- [ ] Form Dialog `.xaml` + `.xaml.cs` in `Views/`
- [ ] Update list ViewModel with New/Edit/action commands
- [ ] Update list View with button bindings and double-click
- [ ] Update list View code-behind with event handlers
- [ ] Test create flow end-to-end (button → dialog → save → API → refresh list)
- [ ] Test edit flow end-to-end (select → edit → dialog → save → API → refresh list)
- [ ] Verify API logs show 200/201 responses (not 422)

### Common Mistakes to Avoid
1. **JSON casing**: ApiClient already uses `JsonNamingPolicy.CamelCase`. C# properties are PascalCase, they serialize to camelCase automatically. Do NOT add `[JsonPropertyName]` attributes.
2. **Nullable decimals**: Money fields should be `decimal?` not `decimal` — empty textbox should send null, not 0.
3. **DatePicker binding**: WPF DatePicker SelectedDate is `DateTime?` — bind directly to nullable DateTime properties.
4. **Dialog ownership**: Always set `Owner = Application.Current.MainWindow` on dialog windows.
5. **Async commands**: Use `[RelayCommand] private async Task MethodAsync()` — the toolkit generates the async command properly.
6. **ComboBox binding for objects**: When binding to a list of model objects (like ClientModel for the job form's client dropdown), use `DisplayMemberPath` and `SelectedValuePath`/`SelectedValue` rather than `SelectedItem` if you only need the ID. Or use `SelectedItem` with a full object reference.
7. **Status filter reload**: When the status filter ComboBox changes, the list should reload. Either handle `SelectionChanged` in code-behind to call `LoadDataCommand`, or use a partial method `OnStatusFilterChanged` (CommunityToolkit generates these for `[ObservableProperty]`).

### Build & Test
After each phase, build the solution and test:
1. Verify no build errors

