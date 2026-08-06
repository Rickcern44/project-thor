## 1. CI Workflow (PR Gate)

- [x] 1.1 Add `.github/workflows/ci.yml` triggered on `pull_request`
- [x] 1.2 Backend job: restore, `dotnet build`, `dotnet test` (unit + Testcontainers integration; runner needs Docker available)
- [x] 1.3 Frontend job: `npm ci`, `npm run check`, `npm run lint`, `npm run test:e2e` (Playwright, with browsers installed in-workflow)
- [x] 1.4 Verify workflow syntax locally (e.g. `actionlint` or `act -n`) since this environment cannot run it on GitHub directly

## 2. GitOps Manifests (`infrastructure/deploy/<service>/`, one Kustomize base per service)

- [x] 2.1 Create Kustomize bases: `infrastructure/deploy/api/kustomization.yaml` and `infrastructure/deploy/web/kustomization.yaml`, each self-contained
- [x] 2.2 `api` `Deployment` manifest (`infrastructure/deploy/api/deployment.yaml`): placeholder image tag, `secretKeyRef` referencing `project-thor-api-secrets` for `ConnectionStrings__Default`, container port matching the Dockerfile's `8080`
- [x] 2.3 `api` `Service` manifest (`infrastructure/deploy/api/service.yaml`)
- [x] 2.4 `web` `Deployment` manifest (`infrastructure/deploy/web/deployment.yaml`): placeholder image tag, container port matching the Dockerfile's `3000`
- [x] 2.5 `web` `Service` manifest (`infrastructure/deploy/web/service.yaml`)
- [x] 2.6 Verify: `kustomize build infrastructure/deploy/api` and `kustomize build infrastructure/deploy/web` each independently produce valid manifests with no hardcoded secrets and no reference to the other service

## 3. API Build & Promote Workflow

- [x] 3.1 Add `.github/workflows/api-build-and-promote.yml`, triggered on `push` to `main`, path-filtered to `src/api/**` only
- [x] 3.2 Run GitVersion action to compute the version for this run
- [x] 3.3 Build and push the `api` image to `ghcr.io`, tagged with the computed version (and `latest`)
- [x] 3.4 Run `kustomize edit set image` in `infrastructure/deploy/api/` to pin that base's image to the computed version
- [x] 3.5 Commit the manifest change to `main` as `github-actions[bot]` (message identifying the version), with a pull/rebase-retry before push to handle an unrelated commit landing on `main` concurrently
- [x] 3.6 Verify workflow syntax locally; confirm the path filter means a `src/web/**`- or `infrastructure/deploy/**`-only commit does not re-trigger this workflow

## 4. Web Build & Promote Workflow

- [x] 4.1 Add `.github/workflows/web-build-and-promote.yml`, triggered on `push` to `main`, path-filtered to `src/web/**` only
- [x] 4.2 Run GitVersion action to compute the version for this run
- [x] 4.3 Build and push the `web` image to `ghcr.io`, tagged with the computed version (and `latest`)
- [x] 4.4 Run `kustomize edit set image` in `infrastructure/deploy/web/` to pin that base's image to the computed version
- [x] 4.5 Commit the manifest change to `main` as `github-actions[bot]` (message identifying the version), with a pull/rebase-retry before push to handle an unrelated commit landing on `main` concurrently
- [x] 4.6 Verify workflow syntax locally; confirm the path filter means a `src/api/**`- or `infrastructure/deploy/**`-only commit does not re-trigger this workflow

## 5. GitHub Remote Setup (manual, user-owned — no GitHub access from this environment)

- [x] 5.1 Create the GitHub repository and push `main`
- [x] 5.2 Enable branch protection on `main` requiring the CI workflow to pass before merge
- [x] 5.3 Confirm the default `GITHUB_TOKEN` has `packages: write` (Settings → Actions → Workflow permissions), needed for pushing to `ghcr.io`

## 6. Homelab Cluster Setup (manual, user-owned — no cluster access from this environment)

- [x] 6.0 Stand up Postgres as a standalone container outside the k8s cluster (per D10 — not an in-cluster `StatefulSet`), on stable homelab host storage; note the resulting host/port for 6.1 — user-confirmed, running at 192.168.86.44:5432, database `thor`
- [x] 6.1 Create the `project-thor-api-secrets` Secret on the cluster with the real `ConnectionStrings__Default` value pointing at the standalone Postgres container from 6.0 — user-confirmed
- [x] 6.2 Register both ArgoCD `Application` resources on the cluster (`kubectl apply -f infrastructure/argocd/api-application.yaml -f infrastructure/argocd/web-application.yaml`, or the `argocd app create` equivalent), confirming the `destination.namespace` and `destination.server` match the target cluster/namespace — user-confirmed
- [x] 6.3 Confirm ArgoCD has pull access to this GitHub repo (public repo: none needed; private: configure ArgoCD repo credentials) — repo is public, no credentials needed

## 7. End-to-End Verification

- [x] 7.1 Open a PR with a trivial change; confirm `ci.yml` runs and blocks/allows merge correctly
- [x] 7.2 Merge an `src/api`-only change to `main`; confirm only `api-build-and-promote.yml` runs, the `api` image appears in `ghcr.io`, and the tag-bump commit updates only `infrastructure/deploy/api/` — required two fixes first: (1) Docker build context was scoped to `src/api`, hiding the sibling `src/ServiceDefaults` project `Api.csproj` depends on, so every build since the Aspire AppHost change failed (#12); (2) the bot's tag-bump push can't bypass the `main` ruleset on a personal repo (GitHub Apps aren't eligible bypass actors there, only roles), so it now pushes via an admin PAT (`PR_BYPASS_TOKEN`) instead of `GITHUB_TOKEN` (#13, typo fixed in #15). Verified end-to-end in PR #16: image pushed as `0.0.30-2`, tag-bump commit `daa03ca` landed cleanly.
- [x] 7.3 Merge an `src/web`-only change to `main`; confirm only `web-build-and-promote.yml` runs, the `web` image appears in `ghcr.io`, and the tag-bump commit updates only `infrastructure/deploy/web/` — verified in PR #17 (`feat(web): set the browser tab title`, first-ever fire of this workflow): image pushed as `0.0.32-2`, tag-bump commit `0b11bc4` touched only `infrastructure/deploy/web/kustomization.yaml`, `api-build-and-promote.yml` did not run
- [x] 7.4 (Optional) Merge a change touching both `src/api` and `src/web` in one commit; confirm both workflows run and both tag-bump commits land, each in its own directory — verified in PR #18 (fixed a real bug: health check's `Status` field always said "healthy" regardless of `DatabaseReachable`, plus an `app.html` meta description): both workflows fired from the single merge commit, api tag-bumped to `0.0.34-2` (`8482db5`, api dir only) and web to `0.0.34-2` (`dad3f41`, web dir only)
- [x] 7.5 (User-confirmed, cluster access required) Confirm each ArgoCD `Application` detects its own tag-bump commit and syncs independently — an `api`-only tag bump should show a pending sync on `project-thor-api` only, not `project-thor-web` — user confirmed both Applications Synced/Healthy; also structurally guaranteed by each Application's `spec.source.path` scoping to its own directory
- [x] 7.6 (User-confirmed) Confirm `GET /health` on the deployed `api` reports `databaseReachable: true` — user confirmed
- [x] 7.7 Validate every scenario in `specs/ci-cd/spec.md` against the actual workflow runs and manifests — all 11 scenarios confirmed: PR gate blocks failing checks (proved for real via throwaway PR #19 — `gh pr merge` refused with "the base branch policy prohibits the merge", then closed without merging) and allows passing ones (demonstrated repeatedly, PRs #12–#18); per-service image builds are independent (7.2/7.3/7.4); manifest-only commits (the bot's own tag-bump commits) never trigger either promote workflow, confirmed against actual run history; both `infrastructure/deploy/api` and `infrastructure/deploy/web` build independently via `kustomize build`, each pinned to an exact version tag (`0.0.34-2`, never `latest`), with no cross-service references and the api Deployment's `secretKeyRef` to `project-thor-api-secrets` intact
