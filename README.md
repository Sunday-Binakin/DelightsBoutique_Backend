# Delight's Boutique — Backend

ASP.NET Core 8 backend for Delight's Boutique. See [BUILD_SPEC.md](BUILD_SPEC.md) for the
full architecture, data model, API surface, and build roadmap this project follows.

## Prerequisites

- **.NET 8 SDK.** This repo is pinned to .NET 8 via `global.json`. If your machine's default
  `dotnet` on PATH is a different major version (check with `dotnet --version`), either:
  - install the .NET 8 SDK system-wide, or
  - if it's installed per-user only (e.g. via the `dotnet-install` script into
    `%LOCALAPPDATA%\Microsoft\dotnet`), add that directory to the **front** of your `PATH`
    so it's found before any other `dotnet` installation.
- **PostgreSQL 16+** running locally (or via Docker — see below).
- **Node/Docker** are not required to run the API itself; Docker is only needed if you want
  to run Postgres in a container instead of a local install, or to build the container image.

## Local database setup (first time only)

The app connects as a scoped, least-privilege role rather than the Postgres superuser
(per BUILD_SPEC.md §1, Least Privilege). As the postgres superuser, run once:

```sql
CREATE DATABASE delightsboutique_dev;
CREATE ROLE delightsboutique_app LOGIN PASSWORD '<choose a password>';
GRANT CONNECT ON DATABASE delightsboutique_dev TO delightsboutique_app;
-- then, connected to delightsboutique_dev:
GRANT USAGE ON SCHEMA public TO delightsboutique_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO delightsboutique_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO delightsboutique_app;
```

This grants CRUD on tables but **not** DDL (`CREATE TABLE`, etc.) — migrations (from Phase 1
onward) need to run as a more privileged role (the superuser is fine for local dev); the app
itself only ever connects as `delightsboutique_app`.

Then store the connection string in user-secrets (never committed to the repo):

```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=delightsboutique_dev;Username=delightsboutique_app;Password=<password>" --project src/DelightsBoutique.Api
```

## Running

```powershell
dotnet run --project src/DelightsBoutique.Api
```

Swagger UI is available at `/swagger` in Development. `GET /api/health` returns `{ "ok": true }`
and currently does not check DB connectivity — that's added once `AppDbContext` exists (Phase 1).

## Running with Docker Compose

```powershell
docker compose up --build
```

Spins up Postgres + the API together, matching what Staging/Production run. Not required for
day-to-day local development if Postgres is already installed locally.

## Tests

```powershell
dotnet test tests/DelightsBoutique.UnitTests
dotnet test tests/DelightsBoutique.IntegrationTests   # requires Docker (Testcontainers)
```

## Solution layout

See BUILD_SPEC.md §3 for the full rationale. Summary: `Domain` → no dependencies;
`Application` → depends on `Domain`; `Infrastructure` → depends on `Application` + `Domain`;
`Api` → depends on `Application` + `Infrastructure` (wiring only, in `Program.cs`).
