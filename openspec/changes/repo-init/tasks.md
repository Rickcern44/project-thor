## 1. Git & Workflow Foundations

- [x] 1.1 `git init`, add root `.gitignore` (covers .NET, Node, IDE, Docker artifacts)
- [ ] 1.2 Write `CONTRIBUTING.md` documenting branch-per-spec, commit-per-task, and Conventional Commits format
- [ ] 1.3 Add root `.editorconfig`

## 2. Backend Scaffold (Vertical Slice Architecture)

- [ ] 2.1 Create `src/api` .NET 10 solution and web project
- [ ] 2.2 Create `Features/` root; remove/avoid default `Controllers/Services/Repositories` scaffolding
- [ ] 2.3 Implement one example vertical slice: `Features/Health/` with a `/health` endpoint (request → handler → endpoint colocated)
- [ ] 2.4 Add `dotnet format` configuration for the backend
- [ ] 2.5 Verify: `dotnet build` succeeds and `GET /health` returns success

## 3. EF Core + PostgreSQL Wiring

- [ ] 3.1 Add EF Core + Npgsql packages; create empty `AppDbContext` in a shared `Data`/`Infrastructure` project
- [ ] 3.2 Configure connection string resolution via environment variable / `dotnet user-secrets` (no hardcoded secrets)
- [ ] 3.3 Add EF Core design-time factory for migration tooling
- [ ] 3.4 Generate baseline migration; verify `dotnet ef database update` applies cleanly against a local Postgres
- [ ] 3.5 Verify: fresh database + baseline migration produces the migrations history table with no domain tables

## 4. Backend Test Harness

- [ ] 4.1 Create xUnit v3 unit test project; add one trivial passing unit test
- [ ] 4.2 Create xUnit v3 integration test project; add Testcontainers PostgreSQL dependency
- [ ] 4.3 Write one integration test proving: container starts, migrations apply, connection succeeds
- [ ] 4.4 Verify: `dotnet test` passes both projects from a clean clone

## 5. Frontend Scaffold (SvelteKit + PWA)

- [ ] 5.1 Scaffold `src/web` via SvelteKit 5 CLI (TypeScript)
- [ ] 5.2 Add TailwindCSS via official Svelte integration
- [ ] 5.3 Establish component-based structure (`lib/components/`, file-based `routes/`)
- [ ] 5.4 Add `@vite-pwa/sveltekit` (or equivalent) with `generateSW` strategy; configure manifest (name, icons, theme)
- [ ] 5.5 Add placeholder app icons for manifest requirements
- [ ] 5.6 Add ESLint + Prettier configuration
- [ ] 5.7 Verify: production build serves a valid manifest and registers a Service Worker with no console errors

## 6. Frontend Test Harness

- [ ] 6.1 Add Playwright, configure `webServer` to boot the SvelteKit dev server automatically
- [ ] 6.2 Write one trivial passing test (e.g. home page renders)
- [ ] 6.3 Verify: Playwright test command passes from a clean clone with no manual server startup

## 7. Docker & Local Dev Environment

- [ ] 7.1 Write multi-stage `src/api/Dockerfile`
- [ ] 7.2 Write multi-stage `src/web/Dockerfile`
- [ ] 7.3 Write root `docker-compose.yml` wiring api + web + Postgres, with env vars matching section 3.2
- [ ] 7.4 Verify: `docker-compose up` starts all three services and the API successfully connects to Postgres

## 8. Versioning

- [ ] 8.1 Add `GitVersion.yml` configured for Mainline mode
- [ ] 8.2 Verify: `dotnet-gitversion` runs against the repo and outputs a computed version with no config errors

## 9. Final Verification

- [ ] 9.1 From a clean clone (or clean checkout), run the full sequence: backend build → backend tests → frontend build → frontend tests → `docker-compose up` → confirm all succeed
- [ ] 9.2 Validate every scenario in `specs/repo-init/spec.md` against the actual repo state
- [ ] 9.3 Commit with Conventional Commit message per `CONTRIBUTING.md`
