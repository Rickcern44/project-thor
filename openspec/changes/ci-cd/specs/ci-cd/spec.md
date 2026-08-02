## ADDED Requirements

### Requirement: Pull requests are gated by automated checks
Every pull request SHALL trigger a GitHub Actions workflow that runs backend build/test and frontend build/test/lint, and the pull request SHALL be blocked from merging while that workflow is failing.

#### Scenario: PR with a failing backend test is blocked
- **WHEN** a pull request is opened with a failing `dotnet test`
- **THEN** the CI workflow reports failure and the PR cannot be merged

#### Scenario: PR with all checks passing is mergeable
- **WHEN** a pull request has passing backend and frontend checks
- **THEN** the CI workflow reports success and the PR is mergeable

### Requirement: Merges to main produce versioned, published images, per service
Every merge to `main` that touches `src/api/**` SHALL trigger a workflow that computes a version with GitVersion, builds the `api` Docker image, and pushes it to `ghcr.io` tagged with that version. Every merge to `main` that touches `src/web/**` SHALL do the same for the `web` Docker image, via an independent workflow.

#### Scenario: API code change on main produces a new API image only
- **WHEN** a commit touching only `src/api` is merged to `main`
- **THEN** a new `api` image tagged with the GitVersion-computed version is pushed to `ghcr.io`, and no `web` image is built or pushed

#### Scenario: Web code change on main produces a new web image only
- **WHEN** a commit touching only `src/web` is merged to `main`
- **THEN** a new `web` image tagged with the GitVersion-computed version is pushed to `ghcr.io`, and no `api` image is built or pushed

#### Scenario: Manifest-only change does not rebuild either image
- **WHEN** a commit touching only `infrastructure/deploy/**` lands on `main`
- **THEN** neither the `api` nor the `web` build/push workflow runs

### Requirement: Deployment manifests reference immutable image tags
`infrastructure/deploy/api/` and `infrastructure/deploy/web/` SHALL each reference their own image by an exact version tag and SHALL NOT reference the `latest` tag.

#### Scenario: Manifest inspection shows a pinned version
- **WHEN** the `Deployment` manifests under `infrastructure/deploy/api/` and `infrastructure/deploy/web/` are inspected after a promote run
- **THEN** each image reference is an exact version tag (e.g. `0.1.7`), never `latest`

### Requirement: Successful image push updates deployment manifests automatically, per service
After a successful image push for a service, that service's workflow SHALL update the image tag in that service's own `infrastructure/deploy/<service>/` directory using Kustomize and commit the change to `main` without manual intervention, without touching the other service's manifest directory.

#### Scenario: Tag bump follows a successful push
- **WHEN** the `api` image is pushed successfully for a given version
- **THEN** `infrastructure/deploy/api/` is updated to reference that version and committed to `main` by an automated identity, and `infrastructure/deploy/web/` is untouched

### Requirement: GitOps deployment manifests are independent per service
`infrastructure/deploy/api/` and `infrastructure/deploy/web/` SHALL each be a self-contained Kustomize base (own `kustomization.yaml`, `Deployment`, and `Service`) buildable on its own, and the `api` `Deployment` SHALL reference its database connection string via a Kubernetes `Secret` rather than a hardcoded value.

#### Scenario: Each service's manifests build with Kustomize independently
- **WHEN** `kustomize build infrastructure/deploy/api` and `kustomize build infrastructure/deploy/web` are each run on their own
- **THEN** each produces valid Kubernetes manifests for that service alone, with no hardcoded connection string and no reference to the other service's resources

#### Scenario: API Deployment declares its secret dependency
- **WHEN** the `api` `Deployment` manifest is inspected
- **THEN** it references a `Secret` (`project-thor-api-secrets`) for `ConnectionStrings__Default` via `secretKeyRef`

### Requirement: Each service syncs to the cluster via its own ArgoCD Application
The repo SHALL contain a reference ArgoCD `Application` manifest per service, each pointing `spec.source.path` at that service's own `infrastructure/deploy/<service>/` directory with automated sync enabled, so that a tag-bump commit to one service's directory triggers a sync of only that service's `Application`.

#### Scenario: API tag bump does not affect the web Application's sync
- **WHEN** `infrastructure/deploy/api/` is updated by a promote commit
- **THEN** only the `project-thor-api` `Application` has new changes to sync; the `project-thor-web` `Application` reports no diff
