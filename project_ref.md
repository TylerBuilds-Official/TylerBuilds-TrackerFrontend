# TylerBuilds JobTracker — Project Reference

## Overview
Desktop business management app for TylerBuilds LLC. Tracks clients, jobs, invoices, and revenue. Serves dual purpose: real daily-use tool and data source for TylerBuilds | Assistant demo platform.

## Stack
- **Frontend:** C# / WPF / .NET 8.0 (SDK 10) / MaterialDesignInXAML
- **Backend:** Python / FastAPI (hosted on Carl)
- **Database:** MySQL (hosted on Carl, schema: `TylerBuildsTracker`)
- **Auth:** Azure AD (Entra ID) — single tenant, public client, MSAL interactive browser flow
- **Solution format:** .slnx

## Architecture

### Frontend (WPF App)
```
JobTrackerFrontend/
├── App.xaml / App.xaml.cs       — Composition root: wires services, VMs, navigation
├── MainWindow.xaml              — App shell: login screen, collapsible sidebar, content area
├── appsettings.json             — API base URL, Azure AD config
├── Models/                      — API response DTOs
│   ├── UserModel.cs
│   ├── ClientModel.cs
│   ├── ContactModel.cs
│   ├── JobModel.cs
│   ├── InvoiceModel.cs
│   ├── LineItemModel.cs
│   ├── RevenueSummaryModel.cs
│   ├── JobPipelineModel.cs
│   └── RecentActivityModel.cs
├── Services/
│   ├── AppConfig.cs             — Loads appsettings.json
│   ├── AuthService.cs           — MSAL login/logout/token caching
│   ├── ApiClient.cs             — HTTP client with auto bearer token
│   └── NavigationService.cs     — View factory registry, observable CurrentView
├── ViewModels/
│   ├── MainWindowViewModel.cs   — Sidebar state, nav commands, login/logout
│   ├── DashboardViewModel.cs    — Revenue summary, pipeline, recent activity
│   ├── ClientsViewModel.cs      — Client list
│   ├── JobsViewModel.cs         — Job list with status filter
│   └── InvoicesViewModel.cs     — Invoice list with status filter
├── Views/
│   ├── DashboardView.xaml       — Revenue cards, pipeline, activity feed
│   ├── ClientsView.xaml         — DataGrid with client list
│   ├── JobsView.xaml            — DataGrid with status filter
│   └── InvoicesView.xaml        — DataGrid with status filter
├── Converters/
│   ├── InverseBoolToVisibilityConverter.cs
│   ├── NavWidthConverter.cs
│   └── EntityTypeToIconConverter.cs
└── Resources/                   — Styles, themes, brushes (empty, ready for use)
```

### Patterns
- **MVVM** with CommunityToolkit.Mvvm (ObservableObject, ObservableProperty, RelayCommand)
- **Navigation:** NavigationService holds view factories keyed by string. MainWindowViewModel calls NavigateTo(). ContentControl binds to Navigation.CurrentView.
- **Auth flow:** App opens → login screen shown → user clicks Sign In → MSAL browser popup → token acquired → IsAuthenticated flips → sidebar + content area shown → DashboardView loads
- **Data flow:** View.OnLoaded → ViewModel.LoadDataCommand → ApiClient.GetAsync (attaches bearer token) → FastAPI → Stored Procedure → MySQL

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
    │   ├── client/              — create_client_request, update_client_request, client_response
    │   ├── contact/             — create_contact_request, update_contact_request, contact_response
    │   ├── job/                 — create_job_request, update_job_request, update_job_status_request, job_response
    │   ├── invoice/             — create/update invoice + line item requests, invoice_response, line_item_response
    │   ├── dashboard/           — revenue_summary, monthly_revenue, job_pipeline, recent_activity responses
    │   └── user/                — user_response
    ├── db/
    │   ├── connection/
    │   │   ├── pool.py          — MySQL connection pool (sync, mysql-connector-python)
    │   │   └── client.py        — DatabaseClient: call_proc, call_proc_single, call_proc_scalar
    │   └── tools/               — One file per entity: thin wrappers calling stored procedures
    ├── middleware/
    │   └── auth.py              — Azure AD JWT validation, user upsert, require_auth dependency
    └── services/                — Business logic: client, contact, job, invoice, dashboard services
```

### API Endpoints
| Route | Methods | Auth | Notes |
|-------|---------|------|-------|
| /users/me | GET | Yes | Returns authenticated user profile |
| /clients | GET, POST | Yes | GET supports ?active_only=true |
| /clients/{id} | GET, PUT | Yes | |
| /clients/{id}/deactivate | PATCH | Yes | Soft delete |
| /contacts | POST | Yes | |
| /contacts/{id} | GET, PUT | Yes | |
| /contacts/{id}/remove | PATCH | Yes | Data scrub |
| /contacts/by-client/{id} | GET | Yes | |
| /jobs | GET, POST | Yes | GET supports ?status= filter |
| /jobs/{id} | GET, PUT | Yes | |
| /jobs/{id}/status | PATCH | Yes | |
| /jobs/by-client/{id} | GET | Yes | |
| /invoices | GET, POST | Yes | GET supports ?status= filter |
| /invoices/{id} | GET, PUT | Yes | |
| /invoices/{id}/status | PATCH | Yes | Auto-sets paidDate on Paid |
| /invoices/{id}/line-items | GET | Yes | |
| /invoices/by-job/{id} | GET | Yes | |
| /invoices/line-items | POST | Yes | Auto-recalculates invoice total |
| /invoices/line-items/{id} | PUT | Yes | Auto-recalculates invoice total |
| /dashboard/revenue-summary | GET | Yes | ?year= optional |
| /dashboard/monthly-revenue | GET | Yes | ?year= optional |
| /dashboard/job-pipeline | GET | Yes | |
| /dashboard/recent-activity | GET | Yes | ?limit= optional |

## Azure AD Configuration
- **App Registration:** TylerBuildsJobTracker
- **Client ID:** 494adb8c-a25f-44fa-b067-aea39e35a6dd
- **Tenant ID:** eb39e2f6-db39-4bfc-af4d-69d5e098501a
- **Type:** Public client / Native, single tenant
- **Redirect URI:** http://localhost
- **API Scope:** api://494adb8c-a25f-44fa-b067-aea39e35a6dd/access_as_user
- **Public client flows:** Enabled

## Infrastructure
- **Carl:** Bare metal Linux Mint server — hosts MySQL and FastAPI on LAN
- **Dev machine:** Windows 11 + RTX 4070 — runs WPF app, Rider (C#), PyCharm (Python)
- **API runs on Carl:** `uvicorn main:app --host 0.0.0.0 --port 8000`

## Conventions
- **SQL:** PascalCase tables, camelCase columns, Entity_PascalCaseAction procs. No snake_case.
- **Python:** snake_case functions/variables, PascalCase classes, single responsibility per file
- **C#:** PascalCase properties/methods, _camelCase private fields, one ViewModel per View
- **No emojis anywhere in the app** — use MaterialDesign PackIcons

## What's Next (TODO)
- [ ] CRUD forms in WPF (create/edit dialogs for clients, jobs, invoices)
- [ ] Detail views (click a client → see their jobs and contacts)
- [ ] Invoice PDF generation
- [ ] Expenses table and module
- [ ] TylerBuilds | Assistant tooling hooks into same API/procs
- [ ] Mock data schema (TylerBuildsTrackerDemo) for client demos
- [ ] App theming / custom branding refinement
