## Context

This is a greenfield repo — no code, no git history. `repo-init` is the only change that gets to decide project *structure* for free; every change after it (`ci-cd` and the six domain slices: `user-accounts`, `roster-import`, `game-scheduling`, `game-signup`, `payment-tracking`, `notifications`) will build inside whatever shape this change creates. The stack is fixed by project config: C# .NET 10 + EF Core + PostgreSQL on the backend (Vertical Slice Architecture), SvelteKit 5 + TailwindCSS on the frontend (component-based), xUnit v3 + Playwright for testing, Docker for packaging, GitHub Actions + GitVersion for CI/versioning (pipeline wiring itself is out of scope — see `ci-cd`).

## Goals / Non-Goals

**Goals:**
- A fresh `git clone` builds and passes tests for both `src/api` and `src/web` with no undocumented setup steps.
- Establish the Vertical Slice Architecture skeleton so every future backend feature slice has an obvious, consistent place to land.
- Wire EF Core to PostgreSQL end-to-end (connection, migration tooling) without committing to any domain schema.
- Ship an installable PWA shell (manifest + service worker) so "installable" is true from the first commit, not bolted on later.
- `docker-compose up` brings up api + web + Postgres together for local development.
- Document the git/commit workflow (branch-per-spec, commit-per-task, Conventional Commits) so it's discoverable, not tribal knowledge.

**Non-Goals:**
- No domain schema or entities (users, games, sign-ups, ...) — those arrive with their owning feature-slice change.
- No CI/CD pipeline, no image publishing, no deployment — that's the `ci-cd` change.
- No Kubernetes manifests — deploy strategy onto the local cluster is decided later.
- No authentication, authorization, or business logic.
- No enforcement tooling (commit-msg hooks, required CI checks) — conventions are documented here; enforcement is `ci-cd`'s job.

## Decisions

### D1. Monorepo: `src/api` (.NET) + `src/web` (SvelteKit)
One repo, one OpenSpec store, one version history across stack boundaries. **Why:** feature slices in this domain are inherently full-stack (e.g. "game sign-up" touches both an API endpoint and a UI flow), so a change that spans both projects should be one commit, not a coordinated pair of PRs across repos. *Alternatives:* separate `api`/`web` repos — rejected, adds cross-repo versioning/coordination overhead this project doesn't need yet.

### D2. Backend: Vertical Slice Architecture skeleton, not layered
The API project gets a `Features/` root instead of `Controllers/`, `Services/`, `Repositories/` layers. Each future feature slice (e.g. `Features/GameSignup/`) will colocate its request, handler, and endpoint. `repo-init` creates the convention and one real example slice (a `/health` check) proving the pattern end-to-end — it does not create any domain slices. **Why:** matches the project's stated architecture and mirrors the OpenSpec change decomposition (one vertical slice in code per one OpenSpec change in spec). *Alternatives:* traditional layered architecture — rejected, explicitly not the chosen pattern; scaffolding all six domain slices as empty folders now — rejected, premature structure for slices that don't exist yet.

### D3. EF Core + Postgres: shared `DbContext`, empty schema, real migration proven
A single `AppDbContext` lives in a shared `Data`/`Infrastructure` project referenced by feature slices. `repo-init` wires the Postgres connection (via `appsettings`/environment variables, with local secrets via `dotnet user-secrets`) and proves `dotnet ef migrations` works by generating one trivial migration (e.g. an empty baseline). No domain entities are added here. **Why:** proves the whole migration pipeline (design-time factory, connection string resolution, `dotnet ef database update`) works before any feature slice depends on it. *Alternatives:* skip the baseline migration and let the first domain change prove it — rejected, that couples "does EF Core setup work" to "does user-accounts work," making failures harder to diagnose.

### D4. Frontend: SvelteKit 5 + TailwindCSS + PWA plugin
Scaffold via the official SvelteKit CLI with TypeScript and the Tailwind add-on. PWA shell (manifest + Service Worker) via `@vite-pwa/sveltekit`, configured with a minimal `generateSW` strategy (precache the app shell) rather than a hand-written service worker. **Why:** the Vite PWA plugin is the standard, low-maintenance way to get manifest + SW + installability without hand-rolling cache logic; a minimal precache strategy is enough to satisfy "installable" without deciding an offline-data strategy prematurely (no domain data exists yet to cache). *Alternatives:* hand-written Service Worker — rejected, more code to maintain for no benefit at this stage; defer PWA shell to a later change — rejected per proposal, installability is a foundational, cross-cutting property, not a feature.

### D5. Frontend structure: component-based, standard SvelteKit conventions
`src/web/src/lib/components/` for shared components, `src/web/src/routes/` for pages, following SvelteKit's file-based routing. **Why:** no reason to deviate from SvelteKit's own conventions; component-based matches the project's stated frontend architecture directly.

### D6. Integration tests run against real Postgres via Testcontainers
Backend integration tests spin up an ephemeral Postgres container per test run (Testcontainers for .NET) rather than pointing at the `docker-compose` dev database or using EF Core's in-memory provider. **Why:** the project commits to TDD/BDD; an in-memory provider doesn't validate real Postgres behavior (constraints, migrations, JSON columns, etc.), and a shared dev database makes tests order-dependent and non-repeatable. `repo-init` wires this up with one trivial integration test (e.g. "database is reachable and migrations apply") to prove it end-to-end. *Alternatives:* EF Core InMemory provider — rejected, diverges from production database behavior; shared local Postgres — rejected, not repeatable/parallel-safe.

### D7. Playwright targets the SvelteKit dev server locally
Playwright config uses SvelteKit's `webServer` integration to boot the dev server automatically for local test runs. **Why:** zero-config for developers running `npx playwright test` locally. Running against a production build is a `ci-cd` concern (production-like pipeline stage), not repo-init's.

### D8. GitVersion config only, no pipeline wiring
A `GitVersion.yml` is added using **Mainline** mode (every commit to `main` is potentially releasable; feature branches bump based on Conventional Commit prefixes). **Why:** matches the branch-per-spec workflow (short-lived feature branches merging to `main`, not long-lived release branches), and Conventional Commits already give GitVersion the signal it needs to compute bumps automatically. Actually invoking `gitversion` in a pipeline and tagging releases is `ci-cd`'s job. *Alternatives:* GitFlow mode — rejected, assumes long-lived `develop`/`release` branches this project doesn't use.

### D9. Docker: separate Dockerfiles, `docker-compose` for local dev only
`src/api/Dockerfile` and `src/web/Dockerfile` (multi-stage builds), plus a root `docker-compose.yml` wiring api + web + Postgres for local development. No Kubernetes manifests, no production compose overrides. **Why:** local dev parity is in scope for repo-init; actual cluster deployment shape is explicitly undecided (per proposal) and belongs to `ci-cd` or a later infra change.

### D10. Workflow conventions documented in `CONTRIBUTING.md`
Branch-per-spec (one branch per OpenSpec change), commit-per-task (commit after completing each `tasks.md` item), Conventional Commits format — written down, not yet enforced by tooling. **Why:** makes the convention discoverable to anyone (or any agent) picking up a change, without over-investing in enforcement (hooks, required CI checks) before there's a pipeline to enforce it in.

## Risks / Trade-offs

- **Vertical Slice skeleton with only a health-check slice can look over-engineered for what it contains** → Mitigated by keeping the example slice genuinely minimal (one endpoint) — it exists to prove the pattern, not to anticipate future slices.
- **.NET 10 and SvelteKit 5 are both very recent** → library/tooling compatibility gaps are possible (e.g. Testcontainers, EF Core tooling, Vite PWA plugin support). Mitigated by pinning exact versions in the scaffold and noting any workarounds discovered during setup in this change's `tasks.md`.
- **Empty EF Core schema means the "real" migration test is thin** → Accepted; a baseline migration still proves the tooling chain works, and the first meaningful migration arrives with the first domain change.
- **No enforcement of commit/branch conventions yet** → Accepted for now; if this drifts in practice, enforcement (commit-msg hook, PR template, branch naming check) can be added in `ci-cd` without needing to revisit this change.

## Migration Plan

Not applicable in the deploy sense (nothing is running yet). Build-out order:
1. `git init`, `.gitignore`, `CONTRIBUTING.md`.
2. Scaffold `src/api` (.NET 10 solution, Vertical Slice skeleton, health-check slice).
3. Wire EF Core + PostgreSQL, baseline migration, Testcontainers integration test.
4. Scaffold `src/web` (SvelteKit 5, TailwindCSS, PWA plugin, component structure).
5. Wire xUnit v3 and Playwright with one trivial passing test each.
6. Add Dockerfiles + `docker-compose.yml` for local dev.
7. Add `.editorconfig` / `dotnet format` / ESLint + Prettier config, `GitVersion.yml`.
8. Verify from a clean clone: build, test, and `docker-compose up` all succeed.

**Rollback:** trivial — nothing depends on this yet; delete and restart if the scaffold needs to change shape.

## Open Questions

- **PWA icon/branding assets** — placeholder icons are fine for this change; real branding can replace them later without a spec change.
- **Exact Testcontainers version/config for .NET 10** — to be confirmed during implementation since .NET 10 tooling support is still maturing at time of writing.
