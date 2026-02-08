# TylerBuilds JobTracker — Project Reference

## Overview
Desktop business management app for TylerBuilds LLC. Tracks clients, jobs, invoices, and revenue. Serves dual purpose: real daily-use tool and data source for TylerBuilds | Assistant demo platform.

## Stack
- **Frontend:** C# / WPF / .NET 8.0 (SDK 10) / MaterialDesignInXAML / CommunityToolkit.Mvvm
- **Backend:** Python / FastAPI (hosted on Carl)
- **Database:** MySQL (hosted on Carl, schema: `TylerBuildsTracker`)
- **Auth:** Azure AD (Entra ID) — single tenant, public client, MSAL interactive browser flow
- **Solution format:** .slnx

## Architecture

### Frontend (WPF App)
```
JobTrackerFrontend/
├── App.xaml / App.xaml.cs       — Composition root: wires services, VMs, navigation, theme init
├── MainWindow.xaml              — App shell: login screen, collapsible sidebar, top bar, content area
├── appsettings.json             — API base URL, Azure AD config
├── Models/
│   ├── UserModel.cs
│   ├── ClientModel.cs           — Includes PrimaryContact* fields + computed PrimaryContactName
│   ├── ContactModel.cs
│   ├── JobModel.cs
│   ├── InvoiceModel.cs          — Includes computed DisplayNumber
│   ├── LineItemModel.cs
│   ├── RevenueSummaryModel.cs
│   ├── JobPipelineModel.cs
│   ├── RecentActivityModel.cs
│   ├── Create*Request.cs        — CreateClientRequest (with email/phone for Individuals), CreateJobRequest, CreateContactRequest
│   ├── Update*Request.cs        — UpdateClientRequest, UpdateJobRequest, UpdateContactRequest, UpdateInvoiceRequest
│   └── Update*StatusRequest.cs  — UpdateJobStatusRequest, UpdateInvoiceStatusRequest
├── Services/
│   ├── AppConfig.cs             — Loads appsettings.json
│   ├── AuthService.cs           — MSAL login/logout/token caching (%LocalAppData%\TylerBuilds\JobTracker\msal_cache.bin)
│   ├── ApiClient.cs             — HTTP client with auto bearer token (GET, POST, PUT, PATCH)
│   ├── NavigationService.cs     — View factory registry, observable CurrentView
│   └── ThemeService.cs          — Dark/light mode toggle with persistent preference (theme.json)
├── ViewModels/
│   ├── MainWindowViewModel.cs   — Sidebar state, nav commands, login/logout, dark mode toggle
│   ├── DashboardViewModel.cs    — Revenue summary, pipeline, recent activity
│   ├── ClientsViewModel.cs      — List + detail mode: contacts, jobs, invoices for selected client
│   ├── JobsViewModel.cs         — List + detail mode: invoices for selected job, status quick actions
│   ├── InvoicesViewModel.cs     — List with status filter, status quick actions
│   ├── ClientFormViewModel.cs   — Create/edit client dialog (Individual: email/phone fields)
│   ├── JobFormViewModel.cs      — Create/edit job dialog (client/contact dropdowns)
│   ├── InvoiceFormViewModel.cs  — Create/edit invoice dialog
│   └── ContactFormViewModel.cs  — Create/edit contact dialog for company clients
├── Views/
│   ├── DashboardView.xaml       — Revenue cards (4-col grid), pipeline card, activity feed
│   ├── ClientsView.xaml         — List mode: DataGrid | Detail mode: info cards, tabbed contacts/jobs/invoices
│   ├── JobsView.xaml            — List mode: DataGrid with context menu | Detail mode: info cards + invoices list
│   ├── InvoicesView.xaml        — DataGrid with context menu for status changes
│   ├── ClientFormDialog.xaml    — Type selector, name, email/phone (Individual only), address, notes
│   ├── JobFormDialog.xaml       — Client/contact dropdowns, billing, dates, notes
│   ├── InvoiceFormDialog.xaml   — Job selector, amount, dates, notes
│   └── ContactFormDialog.xaml   — First/last name, email, phone, job title, primary toggle
├── Converters/
│   ├── InverseBoolToVisibilityConverter.cs
│   ├── InverseBooleanConverter.cs
│   ├── NavWidthConverter.cs
│   ├── EntityTypeToIconConverter.cs
│   ├── NullToCollapsedConverter.cs
│   ├── NotNullToBoolConverter.cs
│   ├── ActiveNavBrushConverter.cs     — White overlay for active nav item
│   ├── ActiveNavBorderConverter.cs    — Returns transparent (left accent removed)
│   ├── StatusToBrushConverter.cs      — Status → background color badge
│   └── StatusToForegroundConverter.cs — Status → text color for badge
└── Resources/                   — Styles, themes, brushes (reserved)
```

### Patterns
- **MVVM** with CommunityToolkit.Mvvm (ObservableObject, ObservableProperty, RelayCommand, partial methods)
- **Navigation:** NavigationService holds view factories keyed by string. MainWindowViewModel calls NavigateTo(). ContentControl binds to Navigation.CurrentView.
- **Detail views:** ClientsView and JobsView toggle between list and detail mode using IsDetailMode property. Double-click a row → detail view. Back button → list. No separate views/navigation needed.
- **Form dialogs:** Dual constructor pattern (parameterless = create, with model = edit). SaveCommand takes Window parameter for closing. All dialogs are modal (ShowDialog).
- **Status quick actions:** Right-click context menu on Jobs and Invoices DataGrids with Set Status submenu. Uses PATCH /entity/{id}/status endpoint.
- **Auth flow:** App opens → login screen → MSAL browser popup → token acquired → IsAuthenticated flips → sidebar + content shown → DashboardView loads. "Stay signed in" persists token cache.
- **Data flow:** View.OnLoaded → ViewModel.LoadDataCommand → ApiClient.GetAsync (attaches bearer token) → FastAPI → Stored Procedure → MySQL
- **Theme:** ThemeService uses MaterialDesign PaletteHelper for runtime dark/light toggle. Preference persisted to %LocalAppData%\TylerBuilds\JobTracker\theme.json.

### Backend (FastAPI)
```
TylerBuildsTracker_API/
├── main.py                      — Entry: builds and exposes FastAPI app
├── .env                         — DB creds, Azure AD IDs
├── db_ref.md                    — Full database schema + stored procedure reference
└── src/
    ├── app.py                   — TrackerAPI class: builds FastAPI, adds middleware, registers routers
    ├── config.py                — Loads .env, exposes Config class
    ├── lifecycle.py             — Lifespan: DB pool init/shutdown
    ├── api/
    │   ├── API.py               — Router orchestrator: registers all routers on app
    │   └── routers/             — One file per entity (client, contact, job, invoice, dashboard, user)
    ├── dataclasses/             — Pydantic models organized by entity subdirectory
    │   ├── client/              — create/update requests (with email/phone for Individuals), client_response
    │   ├── contact/             — create/update requests, contact_response
    │   ├── job/                 — create/update/status requests, job_response
    │   ├── invoice/             — create/update/status + line item requests, invoice_response, line_item_response
    │   ├── dashboard/           — revenue_summary, monthly_revenue, job_pipeline, recent_activity responses
    │   └── user/                — user_response
    ├── db/
    │   ├── connection/
    │   │   ├── pool.py          — MySQL connection pool (sync, mysql-connector-python)
    │   │   └── client.py        — DatabaseClient: call_proc, call_proc_single, call_proc_scalar
    │   └── tools/               — One file per entity: thin wrappers calling stored procedures
    ├── middleware/
    │   └── auth.py              — Azure AD JWT validation, user upsert, require_auth dependency
    └── services/                — Business logic per entity
        ├── client_service.py    — CRUD + auto-creates/updates primary contact for Individuals
        ├── contact_service.py   — CRUD with primary enforcement
        ├── job_service.py       — CRUD + status transitions
        ├── invoice_service.py   — CRUD + status (auto-sets paidDate) + line items (auto-recalc total)
        └── dashboard_service.py — Revenue summary, monthly revenue, pipeline, recent activity
```

### API Endpoints
| Route | Methods | Notes |
|-------|---------|-------|
| /users/me | GET | Authenticated user profile |
| /clients | GET, POST | GET: ?active_only=true |
| /clients/{id} | GET, PUT | PUT updates primary contact for Individuals |
| /clients/{id}/deactivate | PATCH | Soft delete |
| /contacts | POST | |
| /contacts/{id} | GET, PUT | |
| /contacts/{id}/remove | PATCH | Data scrub |
| /contacts/by-client/{id} | GET | |
| /jobs | GET, POST | GET: ?status= filter |
| /jobs/{id} | GET, PUT | |
| /jobs/{id}/status | PATCH | |
| /jobs/by-client/{id} | GET | |
| /invoices | GET, POST | GET: ?status= filter |
| /invoices/{id} | GET, PUT | |
| /invoices/{id}/status | PATCH | Auto-sets paidDate on Paid |
| /invoices/{id}/line-items | GET | |
| /invoices/by-job/{id} | GET | |
| /invoices/by-client/{id} | GET | |
| /invoices/line-items | POST | Auto-recalculates invoice total |
| /invoices/line-items/{id} | PUT | Auto-recalculates invoice total |
| /dashboard/revenue-summary | GET | ?year= optional |
| /dashboard/monthly-revenue | GET | ?year= optional |
| /dashboard/job-pipeline | GET | |
| /dashboard/recent-activity | GET | ?limit= optional |

All endpoints require Azure AD bearer token.

## UI Design

### Theme
- **Primary:** BlueGrey / **Secondary:** Teal
- **Dark mode:** Toggle in top bar (WeatherSunny ↔ WeatherNight icon)
- **Card radius:** 4px / **Nav item radius:** 2px
- **Sidebar:** PrimaryHueDarkBrush background, flat buttons with white overlay on active

### Sidebar Navigation
- Hamburger toggle (expand/collapse with animation)
- Icons: ViewDashboard, AccountGroup, Briefcase, FileDocumentOutline
- Active state: subtle white overlay background (0x30 alpha), no left accent border
- All buttons: MaterialDesignFlatButton, 18px icons, Height=36, MinWidth=0

### DataGrid Column Sizing
All tables use proportional star widths — no single column hogs space:
- **Clients:** Name(2*), Type(*), Contact(1.5*), Phone(1.2*), Email(2*), City(*), State(0.6*), Jobs(0.6*)
- **Jobs:** Title(2.5*), Client(2*), Status(*), Billing(*), Est. Value(*), Paid(*)
- **Invoices:** Invoice#(1.5*), Client(2*), Job(2*), Status(*), Amount(*), Due Date(*), Paid Date(*)

### Status Badges
Colored pill badges (CornerRadius=8) with StatusToBrushConverter/StatusToForegroundConverter.

## Azure AD Configuration
- **App Registration:** TylerBuildsJobTracker
- **Client ID:** 494adb8c-a25f-44fa-b067-aea39e35a6dd
- **Tenant ID:** eb39e2f6-db39-4bfc-af4d-69d5e098501a
- **Type:** Public client / Native, single tenant
- **Redirect URI:** http://localhost
- **API Scope:** api://494adb8c-a25f-44fa-b067-aea39e35a6dd/access_as_user

## Infrastructure
- **Carl:** Bare metal Linux Mint server — hosts MySQL and FastAPI on LAN
- **Dev machine:** Windows 11 + RTX 4070 — runs WPF app, WebStorm/Rider (frontend), PyCharm (backend)
- **API command:** `uvicorn main:app --host 0.0.0.0 --port 8000 --reload`

## Conventions
- **SQL:** PascalCase tables, camelCase columns, Entity_PascalCaseAction procs. No snake_case.
- **Python:** snake_case functions/variables, PascalCase classes. All money fields use `float` in Pydantic models (not Decimal) for JSON compatibility.
- **C#:** PascalCase properties/methods, _camelCase private fields, one ViewModel per View.
- **No emojis anywhere in the app** — use MaterialDesign PackIcons.
- **JSON serialization:** C# uses camelCase (JsonNamingPolicy.CamelCase). Python Pydantic uses camelCase field aliases.

## What's Next (TODO)
- [ ] Invoice line items UI (backend complete, frontend not wired)
- [ ] Invoice PDF generation
- [ ] Expenses table and module
- [ ] TylerBuilds | Assistant tooling hooks into same API/procs
- [ ] Mock data schema (TylerBuildsTrackerDemo) for client demos
- [ ] App theming / custom branding refinement
