## 1. Git & Workflow Foundations

- [x] 1.1 `git init`, add root `.gitignore` (covers .NET, Node, IDE, Docker artifacts)
- [x] 1.2 Write `CONTRIBUTING.md` documenting branch-per-spec, commit-per-task, and Conventional Commits format
- [x] 1.3 Add root `.editorconfig`

## 2. Backend Scaffold (Vertical Slice Architecture)

- [x] 2.1 Create `src/api` .NET 10 solution and web project
- [x] 2.2 Create `Features/` root; remove/avoid default `Controllers/Services/Repositories` scaffolding
- [x] 2.3 Implement one example vertical slice: `Features/Health/` with a `/health` endpoint (request → handler → endpoint colocated)
- [x] 2.4 Add `dotnet format` configuration for the backend
- [x] 2.5 Verify: `dotnet build` succeeds and `GET /health` returns success

## 3. EF Core + PostgreSQL Wiring

- [x] 3.1 Add EF Core + Npgsql packages; create empty `AppDbContext` in a shared `Data`/`Infrastructure` project
- [x] 3.2 Configure connection string resolution via environment variable / `dotnet user-secrets` (no hardcoded secrets)
- [x] 3.3 Add EF Core design-time factory for migration tooling
- [x] 3.4 Generate baseline migration; verify `dotnet ef database update` applies cleanly against a local Postgres
- [x] 3.5 Verify: fresh database + baseline migration produces the migrations history table with no domain tables

## 4. Backend Test Harness

- [x] 4.1 Create xUnit v3 unit test project; add one trivial passing unit test
- [x] 4.2 Create xUnit v3 integration test project; add Testcontainers PostgreSQL dependency
- [x] 4.3 Write one integration test proving: container starts, migrations apply, connection succeeds
- [x] 4.4 Verify: `dotnet test` passes both projects from a clean clone

## 5. Frontend Scaffold (SvelteKit + PWA)

- [x] 5.1 Scaffold `src/web` via SvelteKit 5 CLI (TypeScript)
- [x] 5.2 Add TailwindCSS via official Svelte integration
- [x] 5.3 Establish component-based structure (`lib/components/`, file-based `routes/`)
- [x] 5.4 Add `@vite-pwa/sveltekit` (or equivalent) with `generateSW` strategy; configure manifest (name, icons, theme)
- [x] 5.5 Add placeholder app icons for manifest requirements
- [x] 5.6 Add ESLint + Prettier configuration
- [x] 5.7 Verify: production build serves a valid manifest and registers a Service Worker with no console errors

## 6. Frontend Test Harness

- [x] 6.1 Add Playwright, configure `webServer` to boot the SvelteKit dev server automatically
- [x] 6.2 Write one trivial passing test (e.g. home page renders)
- [x] 6.3 Verify: Playwright test command passes from a clean clone with no manual server startup

## 7. Docker & Local Dev Environment

- [x] 7.1 Write multi-stage `src/api/Dockerfile`
- [x] 7.2 Write multi-stage `src/web/Dockerfile`
- [x] 7.3 Write root `docker-compose.yml` wiring api + web + Postgres, with env vars matching section 3.2
- [x] 7.4 Verify: `docker-compose up` starts all three services and the API successfully connects to Postgres

## 8. Versioning

- [x] 8.1 Add `GitVersion.yml` configured for Mainline mode
- [x] 8.2 Verify: `dotnet-gitversion` runs against the repo and outputs a computed version with no config errors

## 9. Final Verification

- [x] 9.1 From a clean clone (or clean checkout), run the full sequence: backend build → backend tests → frontend build → frontend tests → `docker-compose up` → confirm all succeed
- [x] 9.2 Validate every scenario in `specs/repo-init/spec.md` against the actual repo state
- [x] 9.3 Commit with Conventional Commit message per `CONTRIBUTING.md`
