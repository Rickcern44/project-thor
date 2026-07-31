## 1. CI Workflow (PR Gate)

- [x] 1.1 Add `.github/workflows/ci.yml` triggered on `pull_request`
- [x] 1.2 Backend job: restore, `dotnet build`, `dotnet test` (unit + Testcontainers integration; runner needs Docker available)
- [x] 1.3 Frontend job: `npm ci`, `npm run check`, `npm run lint`, `npm run test:e2e` (Playwright, with browsers installed in-workflow)
- [x] 1.4 Verify workflow syntax locally (e.g. `actionlint` or `act -n`) since this environment cannot run it on GitHub directly

## 2. GitOps Manifests (`infrastructure/deploy/`)

- [x] 2.1 Create Kustomize base: `infrastructure/deploy/kustomization.yaml`
- [x] 2.2 `api` `Deployment` manifest: placeholder image tag, `envFrom`/`secretKeyRef` referencing `project-thor-api-secrets` for `ConnectionStrings__Default`, container port matching the Dockerfile's `8080`
- [x] 2.3 `api` `Service` manifest
- [x] 2.4 `web` `Deployment` manifest: placeholder image tag, container port matching the Dockerfile's `3000`
- [x] 2.5 `web` `Service` manifest
- [x] 2.6 Verify: `kustomize build infrastructure/deploy` produces valid manifests with no hardcoded secrets

## 3. API Build & Promote Workflow

- [ ] 3.1 Add `.github/workflows/api-build-and-promote.yml`, triggered on `push` to `main`, path-filtered to `src/api/**` only
- [ ] 3.2 Run GitVersion action to compute the version for this run
- [ ] 3.3 Build and push the `api` image to `ghcr.io`, tagged with the computed version (and `latest`)
- [ ] 3.4 Run `kustomize edit set image` in `infrastructure/deploy/` to pin only the `api` image entry to the computed version
- [ ] 3.5 Commit the manifest change to `main` as `github-actions[bot]` (message identifying the version), using a `concurrency` group shared with the `web` promote workflow and a pull/rebase-retry before push to avoid racing a concurrent `web` commit
- [ ] 3.6 Verify workflow syntax locally; confirm the path filter means a `src/web/**`- or `infrastructure/deploy/**`-only commit does not re-trigger this workflow

## 4. Web Build & Promote Workflow

- [ ] 4.1 Add `.github/workflows/web-build-and-promote.yml`, triggered on `push` to `main`, path-filtered to `src/web/**` only
- [ ] 4.2 Run GitVersion action to compute the version for this run
- [ ] 4.3 Build and push the `web` image to `ghcr.io`, tagged with the computed version (and `latest`)
- [ ] 4.4 Run `kustomize edit set image` in `infrastructure/deploy/` to pin only the `web` image entry to the computed version
- [ ] 4.5 Commit the manifest change to `main` as `github-actions[bot]` (message identifying the version), using the same `concurrency` group as the `api` promote workflow and a pull/rebase-retry before push
- [ ] 4.6 Verify workflow syntax locally; confirm the path filter means a `src/api/**`- or `infrastructure/deploy/**`-only commit does not re-trigger this workflow

## 5. GitHub Remote Setup (manual, user-owned — no GitHub access from this environment)

- [ ] 5.1 Create the GitHub repository and push `main`
- [ ] 5.2 Enable branch protection on `main` requiring the CI workflow to pass before merge
- [ ] 5.3 Confirm the default `GITHUB_TOKEN` has `packages: write` (Settings → Actions → Workflow permissions), needed for pushing to `ghcr.io`

## 6. Homelab Cluster Setup (manual, user-owned — no cluster access from this environment)

- [ ] 6.1 Create the `project-thor-api-secrets` Secret on the cluster with the real `ConnectionStrings__Default` value
- [ ] 6.2 Register an ArgoCD `Application` pointing at this repo's `infrastructure/deploy/` path, targeting the correct cluster/namespace, with auto-sync enabled
- [ ] 6.3 Confirm ArgoCD has pull access to this GitHub repo (public repo: none needed; private: configure ArgoCD repo credentials)

## 7. End-to-End Verification

- [ ] 7.1 Open a PR with a trivial change; confirm `ci.yml` runs and blocks/allows merge correctly
- [ ] 7.2 Merge an `src/api`-only change to `main`; confirm only `api-build-and-promote.yml` runs, the `api` image appears in `ghcr.io`, and the tag-bump commit updates only the `api` entry
- [ ] 7.3 Merge an `src/web`-only change to `main`; confirm only `web-build-and-promote.yml` runs, the `web` image appears in `ghcr.io`, and the tag-bump commit updates only the `web` entry
- [ ] 7.4 (Optional) Merge a change touching both `src/api` and `src/web` in one commit; confirm both workflows run and both tag-bump commits land without one clobbering the other
- [ ] 7.5 (User-confirmed, cluster access required) Confirm ArgoCD detects each tag-bump commit, syncs, and the corresponding pod comes up healthy
- [ ] 7.6 (User-confirmed) Confirm `GET /health` on the deployed `api` reports `databaseReachable: true`
- [ ] 7.7 Validate every scenario in `specs/ci-cd/spec.md` against the actual workflow runs and manifests
