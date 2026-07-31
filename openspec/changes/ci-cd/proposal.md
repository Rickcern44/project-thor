## Why

`repo-init` produced a repo that builds, tests, and runs locally — but nothing enforces that automatically on a pull request, and there's no path from "code merged" to "running on the cluster." This change closes that gap: automated checks on every PR, and a GitOps deployment path to the homelab Kubernetes cluster via the ArgoCD instance already running there.

## What Changes

- **CI workflow (GitHub Actions)**: on every pull request, run backend (`dotnet build`, `dotnet test` — unit + Testcontainers integration) and frontend (`svelte-check`, lint, `playwright test`) checks. A PR cannot merge with a red check.
- **Independent, per-service versioned image builds**: two separate workflows, each path-filtered to its own service (`src/api/**` or `src/web/**`). On merge to `main`, a change to one service computes a version with GitVersion, builds and pushes only that service's Docker image to GitHub Container Registry (`ghcr.io`), and bumps only that service's tag — the other service is untouched and does not rebuild or redeploy.
- **GitOps deployment manifests**: Kustomize manifests under `infrastructure/deploy/` for the `api` and `web` Deployments/Services, following the layout convention already used for homelab deployments.
- **Automated tag promotion**: after a successful image push, the responsible workflow updates that service's image tag in `infrastructure/deploy/` via `kustomize edit set image` and commits the change back to `main`. The existing ArgoCD instance on the homelab cluster (already running, not installed by this change) watches that path and syncs automatically — no manual `kubectl apply` step.
- **BREAKING**: N/A — greenfield addition, no existing pipeline to replace.

## Capabilities

### New Capabilities
- `ci-cd`: automated PR gating (build/test/lint) via GitHub Actions, independent versioned Docker image build and push to `ghcr.io` per service on merge to `main`, and GitOps deployment manifests that the homelab ArgoCD instance syncs to the cluster.

### Modified Capabilities
<!-- None — greenfield, no existing specs modified. -->

## Impact

- **Adds** `.github/workflows/` (CI workflow plus one build-and-promote workflow per service) and `infrastructure/deploy/` (Kustomize manifests) to the repo.
- **Depends on `repo-init`**: reuses its build/test commands and Dockerfiles as-is; no changes to `src/api` or `src/web` application code.
- **Requires a GitHub remote to exist.** This change assumes one is already pushed to; it does not create the remote (the user is handling that separately).
- **Requires a one-time ArgoCD `Application` resource** on the homelab cluster pointing at this repo's `infrastructure/deploy/` path. This environment has no access to the homelab cluster, so that registration is a manual step documented in `tasks.md`, not something this change automates.
- **Out of scope for this change**: provisioning production PostgreSQL on the cluster, a staging/prod environment split, secrets-management strategy beyond the minimum needed to pull images and reach the database, and image vulnerability scanning. These are candidates for later hardening changes.
