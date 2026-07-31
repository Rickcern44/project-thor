## Context

`repo-init` proved the build/test/Docker story works locally. This change automates it: GitHub Actions gates pull requests, and merges to `main` flow through to the homelab Kubernetes cluster via the ArgoCD instance already running there. This environment has no network access to the homelab cluster or a GitHub remote yet, so anything requiring cluster or GitHub-org access is a documented manual step in `tasks.md`, not something executed as part of drafting or implementing this change.

Constraints carried over from earlier decisions: GitHub for CI, ghcr.io for the registry, GitOps manifests at `infrastructure/deploy/` (matches the user's existing homelab convention), ArgoCD (not this change) owns actually applying manifests to the cluster.

## Goals / Non-Goals

**Goals:**
- Every pull request runs backend and frontend build/test/lint automatically; a red check blocks merge.
- Every merge to `main` that touches a given service produces a versioned, immutable image for that service pushed to `ghcr.io` — `api` and `web` build, version, and release independently of each other.
- Deployment is GitOps: a workflow updates image tags in `infrastructure/deploy/`, commits, and ArgoCD's existing auto-sync takes it from there — no `kubectl apply` in the pipeline.
- The manifests are complete enough to actually run (Deployments, Services, secret *references*) even though this change doesn't provision the cluster-side secrets or database itself.

**Non-Goals:**
- Provisioning PostgreSQL on the cluster, or deciding whether it's in-cluster vs. an existing homelab instance.
- A staging/prod environment split — one environment (the homelab cluster) for now.
- Secrets management tooling (Sealed Secrets, SOPS, external-secrets) — out of scope; the interface (a plain K8s `Secret` the Deployment references) is defined, creating it is manual.
- Image vulnerability scanning, SBOM generation, or other supply-chain hardening.
- Installing or configuring ArgoCD itself — it already exists on the cluster.

## Decisions

### D1. Three workflows, split by trigger, privilege, and service
`ci.yml` runs on `pull_request` (build + test both stacks, no registry credentials). `api-build-and-promote.yml` and `web-build-and-promote.yml` each run on `push` to `main`, each scoped with a path filter to only its own service (`src/api/**` or `src/web/**` respectively) — build that service's image, push to `ghcr.io`, bump only that service's tag in the shared `infrastructure/deploy/` manifests. **Why:** the PR workflow needs zero write privileges (least privilege, faster, safe to run on forks later if needed); the promote workflows need `packages: write` and `contents: write`, scoped as narrowly as possible. Splitting by service means a `src/web`-only change never rebuilds or redeploys `api` (and vice versa) — each service releases on its own schedule, matching the user's requirement that they be independently releasable. Splitting by trigger (PR vs. push-to-main) also means a manifest-only commit doesn't accidentally re-trigger an image rebuild. *Alternatives:* one combined promote workflow building both images on every `main` push — rejected, forces every merge (even a `web`-only change) to rebuild and re-tag `api` too, coupling their release cadence for no reason. A single workflow with a build matrix keyed off changed paths was also considered — rejected as needless complexity over two plain, independent workflow files for just two services.

### D2. GitVersion runs independently per pipeline run, off shared repo history
Neither promote workflow needs a version until it actually runs. Each of `api-build-and-promote.yml` and `web-build-and-promote.yml` independently invokes GitVersion (matching the `Mainline` config from `repo-init`) against its own trigger commit and uses that SemVer to tag its own image. **Why:** GitVersion's `Mainline` mode computes a version from commit history to `main`, not from changed file paths — so `api`'s and `web`'s version numbers are two views into one shared, monotonically increasing counter, not two independently-seeded sequences. In practice this means version numbers won't be contiguous per service (e.g. `api` might jump `0.1.4` → `0.1.9` while several `web`-only merges land in between) — that's expected and cosmetic, not a defect; each tag is still unique, immutable, and traceable to the commit that produced it. *Alternatives:* per-service tag prefixes (`api-v0.1.4`, `web-v0.1.7`) with GitVersion configured to count commits since the last matching prefix, giving each service its own contiguous counter — more "correct" looking version sequences, but requires two separate GitVersion configs/tag conventions and buys little for a solo homelab project where the version number's only real job is uniqueness + ordering. Revisit if per-service version semantics start mattering (e.g. a public changelog).

### D9. Shared `kustomization.yaml` writes are serialized to avoid races
Both promote workflows edit the same `infrastructure/deploy/kustomization.yaml` (each touching only its own `images:` entry) and commit to `main`. If a single push touches both `src/api/**` and `src/web/**`, both workflows can trigger on the same commit and run concurrently. Each workflow's commit-and-push step uses a GitHub Actions `concurrency` group scoped to a shared key (e.g. `deploy-manifest-commit`) so only one runs its commit step at a time, and pulls/rebases immediately before pushing, retrying once on a non-fast-forward push rejection. **Why:** without this, two workflows racing to read-modify-write the same file can silently drop one service's tag bump. *Alternatives:* a single workflow that always builds/tags both services closes this race trivially, but reintroduces the coupling D1 rejected; serializing the narrow commit step is a smaller trade-off than coupling every release.

### D3. Manifests reference immutable version tags, never `latest`
`infrastructure/deploy/` always pins an exact version (e.g. `0.1.7`), never `:latest`. Each promote workflow still pushes a `latest` tag for its own image alongside the version tag for convenience (manual `docker pull` inspection), but nothing in the GitOps path ever reads it. **Why:** GitOps depends on the manifest being the single source of truth for "what's running" — a mutable tag breaks that (ArgoCD would show "synced" even after the underlying image changed) and makes rollback-by-`git revert` meaningless. *Alternatives:* `latest` + `imagePullPolicy: Always` — rejected, makes deployed state non-reproducible and undermines the whole point of GitOps.

### D4. Images are public on `ghcr.io`; auth via built-in `GITHUB_TOKEN`
Each promote workflow authenticates to `ghcr.io` with the automatically-provided `GITHUB_TOKEN` (`packages: write` permission) — no PAT or extra secret. Images are pushed as public packages. **Why:** this is a personal/homelab project; a public package means the cluster needs zero pull-secret plumbing to consume images, which is one less manual, easy-to-misconfigure step. *Alternatives:* private packages + a `kubernetes.io/dockerconfigjson` pull secret on the cluster — more correct for anything containing real secrets in the image, but nothing here does (config/secrets are injected at runtime via env vars, never baked into the image), so the extra step isn't buying safety. Revisit if that changes.

### D5. GitOps layout: a single Kustomize base, no overlays yet
`infrastructure/deploy/` holds one Kustomize base with `Deployment` + `Service` for `api` and `web`. No `overlays/staging`, `overlays/prod` split yet. **Why:** there is exactly one target environment today (the homelab cluster); adding overlay structure for environments that don't exist is speculative. Kustomize (over Helm) because the promote workflow's only job is a tag bump, and `kustomize edit set image` is a purpose-built one-liner for exactly that — no templating language or chart versioning needed for a single-environment setup. *Alternatives:* Helm chart with `values.yaml` image tag — more machinery than a tag-bump needs today; revisit if multiple environments arrive and templating starts paying for itself.

### D6. Tag bump is a bot commit straight to `main`, guarded by a path filter
After pushing its image, each workflow runs `kustomize edit set image` inside `infrastructure/deploy/` (touching only its own service's entry) and commits as `github-actions[bot]` directly to `main`. Both promote workflows' triggers path-filter to their own service's `src/**` only, so this manifest-only commit does not re-trigger either of them (it doesn't touch `src/api/**` or `src/web/**`). **Why:** simplest possible loop-free automation for a single-environment, single-committer-workflow setup. *Alternatives:* open a PR for the tag bump instead of committing directly — safer for a team (review gate on deploys) but pure overhead for a solo homelab project where the "review" already happened on the app-code PR; can switch to this later if it stops being just you.

### D7. Deployment secrets are referenced, not provisioned
The `api` Deployment references a `Secret` named `project-thor-api-secrets` (key `ConnectionStrings__Default`) via `envFrom`/`secretKeyRef`. This change writes that reference into the manifest but does not create the `Secret` on the cluster — that's a manual `kubectl create secret` step in `tasks.md`, run against the homelab cluster this environment can't reach. **Why:** keeps the manifest complete and honest (it's clear at a glance what the Deployment needs) without pretending to solve secrets management, which is explicitly out of scope (see Non-Goals).

### D8. ArgoCD `Application` registration is a one-time manual step
This change writes an `Application` manifest (or documents the equivalent `argocd app create` command) as a reference artifact, but doesn't apply it — that requires homelab cluster access this environment doesn't have. **Why:** consistent with D7; anything requiring the homelab cluster is documented, not automated from here.

## Risks / Trade-offs

- **Bot commit to `main` outside normal PR review** → Mitigated by D6's scoping: it only ever touches image tags in `infrastructure/deploy/`, is fully auditable in `git log`, and is trivially revertable.
- **Public container images** → Acceptable per D4 since no secrets are baked into images; revisit if that assumption ever changes.
- **No staging environment** → Every merge to `main` ships straight to the only environment. Acceptable for a single-developer homelab project now; the Kustomize base structure (D5) doesn't block adding an overlay later.
- **ArgoCD auto-sync could deploy a build that passed tests but fails at runtime** (e.g. bad connection string) → No mitigation built into this change beyond D3's easy `git revert` rollback; acceptable given Non-Goals exclude staging.
- **This environment can't verify the ArgoCD sync or cluster-side steps end-to-end** → `tasks.md` explicitly separates "verified here" (workflow files, manifest structure, local `kustomize build` / `act` dry-runs where possible) from "must be verified by the user against the real cluster."
- **Two workflows writing to the same `kustomization.yaml`** → Mitigated by D9's concurrency group + rebase-retry; the window is narrow (only same-commit dual-path changes trigger both at once) and the failure mode is a rejected push retried once, not silent data loss.
- **Version numbers aren't contiguous per service** → Cosmetic only, per D2; each tag remains unique and traceable to its source commit.

## Migration Plan

1. Add `ci.yml` (PR gate: backend + frontend build/test/lint).
2. Add `infrastructure/deploy/` Kustomize base (api + web Deployment/Service, placeholder image tags, secret reference).
3. Add `api-build-and-promote.yml` and `web-build-and-promote.yml` (each: GitVersion → build/push its own image → `kustomize edit set image` for its own entry → commit, with concurrency-guarded push).
4. **Manual, user-owned:** create the GitHub remote, push, enable branch protection requiring the CI check.
5. **Manual, user-owned:** create `project-thor-api-secrets` on the homelab cluster.
6. **Manual, user-owned:** register the ArgoCD `Application` pointing at `infrastructure/deploy/` on the homelab cluster.
7. Merge a change to `main` touching one service, confirm only that service's promote workflow runs, its image lands in `ghcr.io`, and the tag-bump commit appears — then the user confirms ArgoCD picks it up and the app comes up on the cluster.

**Rollback:** `git revert` the tag-bump commit on `main`; ArgoCD auto-syncs back to the previous image tag.

## Open Questions

- **Where production PostgreSQL actually lives** (in-cluster vs. an existing homelab instance) — deferred; only the `Secret` interface is defined here.
- **Whether/when a staging overlay is needed** — deferred until there's a second environment to target.
