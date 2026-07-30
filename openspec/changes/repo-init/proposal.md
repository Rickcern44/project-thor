## Why

The project has no repository yet — no git history, no solution/project files, no frontend scaffold, no test harnesses. Every downstream change (CI/CD pipelines, and each domain feature slice) needs a working, conventions-compliant monorepo to build on. This change stands up that foundation once, so later changes add features instead of re-deciding project structure.

## What Changes

- **Initialize the git repository** with `.gitignore`, and document the branch-per-spec / commit-per-task / Conventional Commits workflow (e.g. in `CONTRIBUTING.md`).
- **Monorepo layout**: `src/api` (.NET 10 backend) and `src/web` (SvelteKit 5 frontend) in one repo, with a root `.sln` referencing the backend project(s).
- **Backend scaffold**: .NET 10 solution structured for Vertical Slice Architecture (a `Features/` root, no layered `Controllers/Services/Repositories` split), Entity Framework Core wired to PostgreSQL with a working (empty) `DbContext` and migration tooling — no domain schema yet, that belongs to each feature slice.
- **Frontend scaffold**: SvelteKit 5 project with TailwindCSS, a component-based folder structure, and the PWA shell (web app manifest + registered Service Worker) so it's installable from day one.
- **Test harnesses**: xUnit v3 project wired into the backend solution; Playwright project wired into the frontend, both runnable via a single local command (e.g. `dotnet test`, `npx playwright test`) with one trivial passing test each to prove the wiring.
- **Local dev environment**: Docker + `docker-compose` bringing up `api`, `web`, and a PostgreSQL container together for local development.
- **Tooling & conventions**: `.editorconfig` / `dotnet format` config for the backend, ESLint + Prettier for the frontend, and a `GitVersion.yml` (version computation config only — pipeline wiring is `ci-cd`'s job).
- **BREAKING**: N/A — greenfield, nothing exists yet to break.

## Capabilities

### New Capabilities
- `repo-init`: the scaffolded monorepo itself — solution/project structure, EF Core + Postgres wiring, SvelteKit + PWA shell, test harness wiring, Docker Compose dev environment, and documented git/commit conventions. Treated as a capability so its guarantees (e.g. "a fresh clone builds and tests pass") are spec'd and verifiable like any other.

### Modified Capabilities
<!-- None — greenfield project, no existing specs. -->

## Impact

- **Greenfield build** — no existing code to affect.
- **Establishes the ground every later change stands on**: `ci-cd` depends on the build/test commands defined here; all six domain changes (`user-accounts`, `roster-import`, `game-scheduling`, `game-signup`, `payment-tracking`, `notifications`) depend on the solution structure, `DbContext`/migration tooling, and frontend scaffold produced here.
- **No production infrastructure** — this change only produces a repo that builds and tests locally; deployment pipelines and cluster infra are out of scope (see the upcoming `ci-cd` change).
