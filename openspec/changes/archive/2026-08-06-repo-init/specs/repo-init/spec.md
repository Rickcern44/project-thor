## ADDED Requirements

### Requirement: Monorepo layout
The repository SHALL contain a `src/api` directory for the .NET 10 backend and a `src/web` directory for the SvelteKit 5 frontend, versioned together in a single git repository.

#### Scenario: Fresh clone has both projects
- **WHEN** a developer clones the repository
- **THEN** `src/api` and `src/web` both exist and each contains a buildable project

### Requirement: Backend Vertical Slice Architecture skeleton
The backend SHALL be structured around a `Features/` root rather than layered `Controllers/Services/Repositories` folders, with one working example slice demonstrating the pattern end-to-end.

#### Scenario: Example slice responds
- **WHEN** the API is running and a client requests the example health-check endpoint
- **THEN** the endpoint, defined inside `Features/`, returns a successful response

#### Scenario: No layered folders exist
- **WHEN** the backend project structure is inspected
- **THEN** there are no top-level `Controllers/`, `Services/`, or `Repositories/` folders

### Requirement: EF Core connects to PostgreSQL and migrations apply
The backend SHALL include an `AppDbContext` configured for PostgreSQL and EF Core migration tooling that successfully creates and applies a baseline migration, with no domain entities defined yet.

#### Scenario: Baseline migration applies cleanly
- **WHEN** `dotnet ef database update` is run against a fresh PostgreSQL instance
- **THEN** the baseline migration applies without error and the migrations history table is created

#### Scenario: Connection string resolved from environment
- **WHEN** the API starts with a PostgreSQL connection string supplied via environment variable or user-secrets
- **THEN** the application connects successfully without any connection string hardcoded in source

### Requirement: Frontend scaffold with installable PWA shell
The frontend SHALL be a SvelteKit 5 project using TailwindCSS and a component-based folder structure, with a web app manifest and registered Service Worker making it installable.

#### Scenario: App is installable
- **WHEN** the built frontend is served and opened in a PWA-capable browser
- **THEN** the browser recognizes a valid web app manifest and offers/allows "install to home screen"

#### Scenario: Service Worker registers
- **WHEN** the frontend loads in a browser
- **THEN** a Service Worker registers successfully with no console errors

### Requirement: Backend and frontend test harnesses are wired and passing
The backend SHALL have an xUnit v3 test project including at least one unit test and one Testcontainers-backed PostgreSQL integration test, and the frontend SHALL have a Playwright test project with at least one passing test, each runnable via a single local command.

#### Scenario: Backend tests pass from a clean clone
- **WHEN** a developer runs `dotnet test` from a fresh clone
- **THEN** all backend unit and integration tests pass, including the Testcontainers-backed database test

#### Scenario: Frontend tests pass from a clean clone
- **WHEN** a developer runs the Playwright test command from a fresh clone
- **THEN** the SvelteKit dev server starts automatically and all frontend tests pass

### Requirement: Local development environment via Docker Compose
The repository SHALL include Dockerfiles for the API and web projects and a `docker-compose.yml` that brings up the API, web app, and a PostgreSQL database together for local development.

#### Scenario: Full stack starts locally
- **WHEN** a developer runs `docker-compose up` from a fresh clone
- **THEN** the API, web app, and PostgreSQL containers all start successfully and the API can reach the database

### Requirement: Versioning and formatting tooling configured
The repository SHALL include a `GitVersion.yml` configured for Mainline mode, an `.editorconfig` and `dotnet format` configuration for the backend, and ESLint + Prettier configuration for the frontend.

#### Scenario: GitVersion computes a version
- **WHEN** `dotnet-gitversion` is run against the repository
- **THEN** it outputs a computed semantic version with no configuration errors

#### Scenario: Format checks run cleanly on scaffolded code
- **WHEN** `dotnet format --verify-no-changes` is run on the backend and the frontend linter is run on the frontend
- **THEN** both complete with no violations on the freshly scaffolded code

### Requirement: Documented contribution workflow
The repository SHALL include a `CONTRIBUTING.md` documenting the branch-per-spec workflow, commit-per-task practice, and Conventional Commits format.

#### Scenario: Workflow is discoverable
- **WHEN** a developer or agent opens `CONTRIBUTING.md`
- **THEN** it explains how to name branches per OpenSpec change, when to commit, and the required commit message format
