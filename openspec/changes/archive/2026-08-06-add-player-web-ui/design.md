## Context

`src/web` is a SvelteKit 5 + Tailwind 4 scaffold from `repo-init` with no custom theme, no components beyond a placeholder header, and one static route. `src/api` is fully built and tested — auth (magic link, cookie session), game scheduling, sign-up/waitlist, payment tracking, and notifications all exist behind a REST API, deployed as a separate service from `web` (own Kustomize base, own ArgoCD Application, per `ci-cd`). Web and api are different origins in production.

This design follows a live design-exploration session (`/opsx:explore`) where several directions were mocked as static HTML/CSS and reacted to directly by the user before any code was written. The decisions below capture what was chosen and why, not a first attempt at guessing taste.

## Goals / Non-Goals

**Goals:**
- Ship the player-facing slice of the app for real use: login, see the live game, sign up/cancel, see balance, see notifications.
- A design system (tokens, shell, nav) that the deferred admin-UI change can build on without redoing foundational decisions.
- Correct behavior on both a phone browser and an installed desktop PWA window, at any window size.

**Non-Goals:**
- Any admin screen (roster import, template/game management, promotion, reconciliation, admin balance list). Deferred to a follow-up change.
- Web Push permission UX / iOS home-screen onboarding nudge (already deferred in `add-league-tracker` task 7.3 for the same reason: pure frontend, lower priority than core sign-up).
- Server-side rendering of authenticated content (see D7) — revisit only if it becomes a real problem.

## Decisions

### D1. Theme implemented via Tailwind v4 `@theme` tokens + a `data-theme` attribute strategy
Design tokens (color, spacing scale already default) defined once in `app.css` via Tailwind v4's CSS-based `@theme`, with dark values as the default and light values under `[data-theme="light"]`. Tailwind's `dark:` variant is repointed from the default `prefers-color-scheme` media strategy to `&:where([data-theme="dark"], [data-theme="dark"] *)` via `@custom-variant`, so components can use `dark:` utilities driven by our own attribute rather than only the OS preference. **Why:** the light theme is a real, user-toggleable feature here (per exploration), not just an OS-preference passthrough — the attribute strategy is the only one that supports a manual override. **Alternatives:** pure `prefers-color-scheme` media query (rejected — no manual toggle possible); a `class`-based dark-mode strategy (works identically to the attribute approach; attribute was chosen only because it composes more naturally with a `data-theme` value that's `"light"` | `"dark"` rather than a boolean class).

### D2. Sidebar (desktop) / bottom tab bar (mobile) via CSS container queries
The app shell is one component; a `@container` query (not a viewport `@media` query) swaps navigation pattern and grid layout at ~720px of the shell's own available width. **Why:** confirmed directly with the user — sidebar was chosen over a top-nav alternative after comparing both, and the shell must respond to its own container width because it can run as an installed PWA in an arbitrary window size, not just a mobile browser viewport. A viewport media query would be wrong for a resizable installed window. **Alternatives:** top-nav (mocked, compared, not chosen — sidebar won on "persistent sense of place" and room to grow); viewport media queries (wrong primitive for a resizable window, as above).

### D3. System font stack, no webfont dependency
UI text uses `-apple-system, "Segoe UI", Inter, ui-sans-serif, system-ui, sans-serif`; tabular numerals (roster counts, fees, waitlist position) use `ui-monospace, "SF Mono", "JetBrains Mono", Menlo, Consolas, monospace`. **Why:** zero webfont loading cost/flash, and validated visually during exploration against the actual approved mockups. **Alternatives:** self-hosted Inter/JetBrains Mono via `@font-face` (adds binary assets and a load step for marginal visual gain over the system stack on the target platforms).

### D4. Brand accent kept distinct from semantic color
Orange (`#fb923c` family) is the single brand accent — primary CTA, live-status pulse, active nav state. Paid/confirmed uses a separate green; owed/attention uses a separate amber, both re-tuned per theme. **Why:** on mobile only one orange element was ever visible at once, but the desktop layout puts several in view together (CTA + owed badges + live indicator) — conflating "brand" with "state" would make the owed badge look like a second call-to-action. **Alternatives:** using accent for both brand and "owed" state (this is literally what the first mobile-only mockups did — it read fine there but was corrected once desktop density made the collision visible).

### D5. Theme preference persisted client-side, defaults to system preference
On first visit (no stored preference), theme follows `prefers-color-scheme`. Once the user toggles, the choice is written to `localStorage` and wins on every subsequent load, applied via a small inline script in `app.html` that runs before first paint (sets the `data-theme` attribute synchronously) to avoid a flash of the wrong theme. **Why:** standard, low-risk pattern; avoids FOUC. **Alternatives:** server-persisted preference (would need a user-settings table and an authenticated round-trip before first paint — real cost for a preference that's fine to be per-device).

### D6. `lucide-svelte` for iconography
The exploration mockups hand-authored a handful of inline SVGs (bell, nav icons, checkmark). The full player surface needs more icons than that (auth states, cancel, waitlist, empty states) and hand-authoring each one doesn't scale or stay visually consistent. **Why lucide:** Svelte-native, tree-shakeable (only imported icons ship), MIT-licensed, stroke-based icon style matching the exploration's `stroke-width: 1.8`, `currentColor` SVGs almost exactly. **Alternatives:** continue hand-authoring SVGs (fine for 3–4 icons, not for a full app); Heroicons (also fine, no strong reason over lucide); an icon font (rejected — worse accessibility and tree-shaking than inline SVG components).

### D7. Client-side-only data fetching for authenticated routes in this first slice
Authenticated pages (`/`, `/balance`, `/notifications`, `/profile`) disable SSR (`export const ssr = false`) and fetch from the API directly in the browser with `credentials: 'include'`, relying on the API's existing CORS + cookie-session configuration. **Why:** `web` and `api` are separate origins/deployments; SSR would require the SvelteKit server to forward the browser's session cookie to the API on every server-rendered request, which is real plumbing (a server hook, cookie forwarding, origin config) for a first slice whose primary usage pattern is "open the installed PWA on your phone" where SSR's main benefit (fast first paint before JS loads) matters less than for a public content site. **Alternatives:** SSR with cookie forwarding via `hooks.server.ts` (legitimate, better perceived performance and works without JS momentarily — revisit if login→first-paint latency turns out to matter in practice); a BFF/proxy layer in the SvelteKit server (more infrastructure than this slice needs).

### D8. Route set for this change
`/login` (request magic link), the magic-link consume landing route, `/` (live game — the default/home route), `/balance`, `/notifications`, `/profile` (minimal: name/email, logout). Profile is intentionally thin here — it exists because the nav pattern implies it and logout has to live somewhere, not because profile management is in scope.

### D9. Admin screens are a separate, later change
The admin reconciliation mockup from exploration was a rough first pass, not a reviewed direction like the player screens. Admin is also a materially larger surface (roster import, template management, ad-hoc games, roster add/remove, promotion, waive/pay, admin balance list) that deserves its own scoping pass rather than being bolted onto this change's tasks list.

## Risks / Trade-offs

- **CSR-only auth routes (D7)** → no SSR fast-paint or SEO for authenticated content. Acceptable: this is a private installed PWA, not indexed content. Mitigate with a lightweight loading state so the shell (nav, header) paints immediately while data fetches.
- **Container queries require modern browsers** (Safari 16+, Chrome 105+, Firefox 110+) → acceptable; nothing in this project targets legacy browsers, and the PWA install story already assumes a modern engine.
- **Theme flash on load** → mitigated by the blocking inline script in D5; verify it actually runs before first paint once implemented (easy to get wrong with SvelteKit's hydration order).
- **New dependency (`lucide-svelte`)** → small, tree-shakeable, low risk; verify bundle impact stays negligible once wired up.

## Migration Plan

Greenfield frontend work replacing a placeholder — no data migration. Ships through the existing, already-proven CI/CD pipeline (`web-build-and-promote.yml` → `ghcr.io` → ArgoCD sync). No real users are on the current placeholder, so no rollback plan beyond the normal "revert the merge" path is needed.

## Open Questions

- Notifications: dedicated route vs. a slide-over/panel reachable from the bell icon on any screen? Mockups only show the bell affordance, not the expanded view — leave for task breakdown, not blocking.
- Exact copy for auth emails and empty/error states — not decided during exploration, needs writing during implementation.
- Whether the admin follow-up change can reuse this design system as-is, or needs it extended (data tables, denser forms, bulk actions) — will surface once that change is scoped; not this change's problem to solve preemptively.
