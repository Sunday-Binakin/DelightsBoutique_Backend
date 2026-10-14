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

## Migrations (Phase 1+)

Migrations need DDL privileges the app's own low-privilege role doesn't have, so they run
against a separate, elevated connection string read from an environment variable — never
from `appsettings.json` or user-secrets:

```powershell
$env:DB_MIGRATION_CONNECTION = "Host=localhost;Port=5432;Database=delightsboutique_dev;Username=postgres;Password=<superuser password>"
dotnet ef migrations add <Name> --project src/DelightsBoutique.Infrastructure --startup-project src/DelightsBoutique.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/DelightsBoutique.Infrastructure --startup-project src/DelightsBoutique.Api
```

(On this machine specifically, prepend `$env:LOCALAPPDATA\Microsoft\dotnet` to `PATH` first,
same caveat as above — `dotnet ef` shells out to `dotnet msbuild` via PATH internally, so
even an explicit full path to the outer `dotnet` call isn't enough on its own.)

## Email (OTP login)

OTP codes and order notifications are sent via Gmail SMTP (MailKit, smtp.gmail.com:587,
STARTTLS). Store the sending account's credentials in user-secrets — use a Gmail
[App Password](https://myaccount.google.com/apppasswords), never the account password:

```powershell
dotnet user-secrets set "Gmail:Email" "<sending gmail address>" --project src/DelightsBoutique.Api
dotnet user-secrets set "Gmail:AppPassword" "<app password>" --project src/DelightsBoutique.Api
```

## File storage (product images, payment proofs)

Local development defaults to `LocalFileStorage` (disk, under `App_Data/uploads`) — no setup
needed. Staging/Production use `SpacesFileStorage` (DigitalOcean Spaces, S3-compatible object
storage) instead, since a hosted container's local disk doesn't survive redeploys. Switched via
config, not code — `FileStorage:Provider` set to `"Spaces"` in `appsettings.Staging.json` /
`appsettings.Production.json`. To point a Spaces-backed environment at a real Space, set:

```powershell
dotnet user-secrets set "Spaces:Endpoint" "https://<region>.digitaloceanspaces.com" --project src/DelightsBoutique.Api
dotnet user-secrets set "Spaces:Bucket" "<your space name>" --project src/DelightsBoutique.Api
dotnet user-secrets set "Spaces:AccessKey" "<access key>" --project src/DelightsBoutique.Api
dotnet user-secrets set "Spaces:SecretKey" "<secret key>" --project src/DelightsBoutique.Api
```

(In Staging/Production these are real environment variables, not user-secrets — see the
deployment notes for exact names.) Uploaded files are stored with a public-read ACL and served
directly from Spaces' own URL; `Spaces:PublicBaseUrl` can override that with a custom domain or
DO's CDN-fronted Spaces endpoint instead.

## Running

```powershell
dotnet run --project src/DelightsBoutique.Api --launch-profile https
```

Swagger UI is available at `/swagger` in Development. `GET /api/health` returns `{ "ok": true }`
and does not check DB connectivity yet. On first run in Development, a single `OwnerProfile`
is seeded automatically if none exists — the generated one-time password is printed once to
the console log (`grep` for "Seeded OwnerProfile"); it is not recoverable afterward short of
deleting the row and restarting.

## Running with Docker Compose

```powershell
docker compose up --build
```

Spins up Postgres + the API together, matching what Staging/Production run. Not required for
day-to-day local development if Postgres is already installed locally.

## Tests

```powershell
dotnet test tests/DelightsBoutique.UnitTests
dotnet test tests/DelightsBoutique.IntegrationTests   # requires Docker (Testcontainers) — not yet installed on this machine; runs in CI
```

## Solution layout

See BUILD_SPEC.md §3 for the full rationale. Summary: `Domain` → no dependencies;
`Application` → depends on `Domain`; `Infrastructure` → depends on `Application` + `Domain`;
`Api` → depends on `Application` + `Infrastructure` (wiring only, in `Program.cs`).
