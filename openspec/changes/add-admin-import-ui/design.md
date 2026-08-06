## Context

`src/api`'s roster-import capability is fully built: `POST /admin/import/roster` (parses the CSV, creates `Game`s, flags rows missing email/phone), `GET /admin/import/flagged-rows` (lists pending flagged rows), `POST /admin/import/flagged-rows/{id}/resolve` (creates the real `User`/`RosterRecord`/historical `SignUp`/`Charge` rows for one flagged row), and `POST /admin/invites` (sends that player's first login-link invite). All four are `AdminOnly`. `src/web` (from `add-player-web-ui`) has a design-token system, `AppShell`, theming, and a login/auth-guard pattern for authenticated players — but its guard only checks "is there a session", not role, and there's no admin surface at all yet.

## Goals / Non-Goals

**Goals:**
- A usable Import → Review → Submit wizard for the CSV roster import, replacing raw curl/Postman calls.
- Reuse `add-player-web-ui`'s design tokens, login flow, and auth-guard pattern rather than building a parallel system.
- Make the wizard discoverable for an Admin without requiring them to know/type the URL.

**Non-Goals:**
- Any other admin screen (game/template management, ad-hoc games, roster add/remove, waitlist promotion, payment reconciliation, admin balance list) — still deferred per `add-player-web-ui`'s design.md D9.
- A "resend invite" or "undo a resolved row" action — not built today; the backend has no un-resolve endpoint either.
- Editing the parsed attendance/dues data during review — see D6.
- Merging a duplicate person's history into an existing account — see D10. A collision is blocked, not resolved automatically.

## Decisions

### D1. New `admin/` route with a role-checking guard
A new `src/web/src/routes/admin/+layout.ts` mirrors `add-player-web-ui`'s `(app)/+layout.ts` (`export const ssr = false`, calls `GET /auth/me`), but additionally requires `role === 'Admin'`. Unauthenticated visitors redirect to `/login`; authenticated non-admins redirect to `/` (the player home), rather than a generic error page. This is a plain `admin/` folder, not a `(admin)` route *group* — groups are invisible in the resulting URL, and the wizard needs the literal `/admin/import` path (caught during implementation via a `resolve()` type error once the route was generated). **Why:** the existing `(app)` guard already proves the "call `/auth/me`, redirect on failure" pattern works for this app's CSR-only auth model (D7 of `add-player-web-ui`); this just adds one more condition rather than inventing a new auth mechanism. **Alternatives:** a server-side check via `hooks.server.ts` — rejected for the same reason CSR-only was chosen originally: no cookie-forwarding plumbing exists between this SvelteKit server and the API.

### D2. A minimal `AdminShell`, not the player `AppShell`
The admin section gets its own small shell (title bar + logout), not the sidebar/bottom-tab-bar `AppShell` from `add-player-web-ui`. **Why:** `AppShell`'s nav destinations (Live Game, Balance, Notifications, Profile) are player concepts that don't apply once you're in the admin wizard; forcing the wizard into that chrome would show irrelevant nav items with nowhere useful to point. **Alternatives:** reuse `AppShell` with a conditional nav item set — rejected for now since there's exactly one admin destination; revisit if/when more admin screens land and a real admin nav becomes worth building.

### D3. One discoverability link, not a parallel nav system
`AppShell`'s existing nav list gains a single conditional entry — "Admin", linking to `/admin/import` — shown only when the signed-in user's `role === 'Admin'`. **Why:** without this, an admin has no way to find the wizard except typing the URL from memory; one conditional link is the smallest change that fixes that. **Alternatives:** a separate admin-only login/subdomain — unnecessary; admins already authenticate through the same magic-link flow as players.

### D4. Single route, wizard steps as component state
The whole flow lives at `/admin/import`, with the current step (`'import' | 'review' | 'done'`) held as local component state, not three separate routes. **Why:** the steps share in-memory state (the just-created import summary counts, the loaded review rows) that has no reason to be a bookmarkable URL, and a wizard's steps aren't independently useful destinations. **Alternatives:** three routes (`/admin/import`, `/admin/import/review`, `/admin/import/submit`) with state passed via query params or reloaded per step — more plumbing for no real benefit here.

### D5. Real drop zone, with a file-input fallback
The import step accepts a file via drag-and-drop onto a drop zone (matching what was asked for) and a real `<input type="file" accept=".csv">` for keyboard/click access. Non-`.csv` selections are rejected client-side with a message before ever calling the API. **Why:** drag-and-drop alone is not keyboard-accessible; pairing it with a real file input is the standard accessible pattern. **Alternatives:** drop-zone-only — rejected, fails for keyboard/screen-reader users.

### D6. Review step: Name is editable, Email/Phone are the new required fields, everything else is read-only
Each flagged row's parsed `Name` is pre-filled and editable (spreadsheet typos happen); `Email` and `Phone` are required new inputs — the actual gap this wizard exists to close. `AttendedDates`, `TotalDue`, and `AmountPaid` are shown read-only. **Why:** those three values feed `HistoricalChargeReconciler`'s paid/owed math server-side at resolve time; letting an admin edit them client-side risks silently mismatching what the backend reconciles against, for a case (correcting historical dues data) this wizard was never asked to solve. **Alternatives:** make everything editable — rejected; out of scope and riskier than the ask.

### D7. Submit runs rows sequentially, each as resolve → invite
On submit, each reviewed row is processed one at a time: `POST /admin/import/flagged-rows/{id}/resolve` (per the propsal's decision to invite immediately), then `POST /admin/invites` for the resulting `rosterRecordId`. Each row's status (`pending` / `success` / `error`) updates in place as it completes, so a failure on one row is visible immediately and doesn't block the rest. **Why:** sequential keeps the per-row status UI simple and ordered; the batch sizes here (a season's roster — tens of people, not thousands) don't need the complexity of controlled concurrency for performance. **Alternatives:** `Promise.all` in parallel — rejected; makes partial-failure reporting harder to reason about for no real speed benefit at this scale.

### D8. No new backend endpoints
Both calls submit needs (`resolve`, then `invite`) already exist. No batch endpoint is added — per-row sequencing is required anyway for D7's independent-outcome reporting, so a batch endpoint would just move the loop server-side for no benefit.

### D9. Row selection: a checkbox per row, default unchecked, plus "select all"
Each review row gets a `selected` checkbox, defaulting to unchecked; a header checkbox toggles all currently-loaded rows. Submit only processes rows where `selected` is true (and not already `success`) — everything else is left untouched in the pending queue. **Why:** confirmed with the user — the review queue can grow larger than an Admin wants to process in one sitting, and defaulting to "opt in" means Submit never silently processes more people than intended. This composes cleanly with D4's standing-queue model: an unselected row just shows up again next time the review step loads. **Alternatives:** default-checked with opt-out — rejected per explicit user preference; a hard cap on rows-per-batch — unnecessary, a checkbox already gives full control without an arbitrary limit.

### D10. Duplicate-email pre-check moved into `ResolveFlaggedRowEndpoint`
Both `User.Email` and `RosterRecord.Email` already carry a unique database index (from `add-league-tracker`, pre-existing). Today, resolving two flagged rows that happen to share an email — e.g. two spelling variants of the same person, resolved in different sessions — hits that constraint as a raw, unhandled exception (500), since neither endpoint pre-checks for it. This change adds an explicit check to `ResolveFlaggedRowEndpoint`: look up an existing `RosterRecord`/`User` by the submitted email before inserting, and if one exists, return `409 Conflict` with a clear message ("a player with this email already exists") instead of attempting the insert. The row stays unresolved; the Admin can correct the email or leave it for later. **Why:** "never duplicate what we add" only holds if a collision fails cleanly, not with a raw DB exception — this is the smallest fix that makes that guarantee real, and it reuses the existing per-row error-reporting path (D7) with no new UI concept. **Alternatives:** merge the new attendance history into the existing player instead of blocking — a materially bigger feature (backfilling `SignUp`/`Charge` rows against someone else's already-existing account); explicitly out of scope here, consistent with this change already not touching historical-data reconciliation (D6).

## Risks / Trade-offs

- **N+1 round trips** (2 API calls per row, sequential) → acceptable at expected batch sizes (a season's roster); revisit only if that assumption stops holding.
- **No undo for a wrongly-submitted row** → matches the backend, which has no un-resolve endpoint. Mitigated by the review step's explicit per-row confirmation before submit — the wizard doesn't auto-submit anything.
- **The duplicate-email pre-check (D10) has a race window**: two concurrent resolve calls for the same new email could both pass the pre-check before either insert commits, and the second would still hit the raw unique-constraint 500. Acceptable — this app has a single admin driving the wizard sequentially, not concurrent admins racing the same row; the DB constraint remains the real backstop either way, this just makes the common case clean.
- **Client-side CSV validation is extension-only**, not shape validation → a wrongly-shaped `.csv` still reaches the API and may just produce an unhelpful 0-games/0-flagged result. Re-implementing `CsvRosterParser`'s column-detection client-side would duplicate logic the backend should stay the sole owner of; acceptable for now.

## Migration Plan

Greenfield frontend addition to the existing `src/web` app — no data migration. Ships through the same CI/CD pipeline already proven by `add-player-web-ui` (`web-build-and-promote.yml` → `ghcr.io` → ArgoCD sync).

## Open Questions

- Whether a "resend invite" action is needed for a row that resolved but whose invite email failed to send — not built now; the review/submit flow always tries both in the same action. Revisit if this turns out to happen often in practice.
- Whether the review list needs pagination once the flagged-row queue grows — not addressed now, since a season's import is expected to be tens of rows, not hundreds.
