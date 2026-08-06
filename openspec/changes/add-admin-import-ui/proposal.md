## Why

`src/api`'s roster-import capability (spreadsheet upload → flag rows missing email/phone → admin resolves them → invite sent) is fully built and tested, but has no UI — an admin can only drive it via raw HTTP calls (curl/Postman), with an undocumented CSV column shape and no visibility into what got flagged or why. Now that the player app is live (`add-player-web-ui`), onboarding the season's actual roster is blocked on this being usable by a human, not just by someone reading the endpoint source.

## What Changes

- **New admin-only import wizard** in the existing `src/web` app: **Import → Review → Submit**.
  - **Import**: a drop zone for the CSV plus a season-year input, posted to the existing `POST /admin/import/roster`. Shows a summary of the result (games created, rows flagged, rows skipped as duplicates).
  - **Review**: lists every currently-pending flagged row (`GET /admin/import/flagged-rows`, not scoped to just this session's upload — it's a standing review queue), decoding each row's raw attendance/dues data for display. The admin confirms/edits the parsed name and fills in the email and phone the spreadsheet didn't have — both are required by `ResolveFlaggedRowEndpoint`, even though only email was explicitly asked for.
  - **Submit**: for each *selected* reviewed row, calls the existing `POST /admin/import/flagged-rows/{id}/resolve` (creates the real `Pending` `User` + `RosterRecord` + backfilled historical `SignUp`/`Charge` rows) and then `POST /admin/invites` for that same roster record, so the player's first login-link invite goes out in the same action. Each row's outcome (resolved+invited / failed) is reported individually, so one bad row doesn't block the rest.
- **Row selection for partial submission**: the review table gets a checkbox per row (plus a header "select all"), unchecked by default. Submit only processes checked rows — an Admin is never forced to work through the whole pending queue in one sitting; anything left unchecked simply stays pending for a later session.
- **Duplicate-email protection**: `POST /admin/import/flagged-rows/{id}/resolve` now checks the submitted email against existing players before creating anything, returning a clean `409` ("a player with this email already exists") instead of letting a raw database unique-constraint violation surface as an unhandled 500. Closes a real gap found while reviewing idempotency: games already dedupe by date and rows already dedupe by name at import time, but nothing previously guarded against two *different* flagged rows resolving to the same email.
- **New role-gated admin route group** in `src/web`: the existing `add-player-web-ui` auth guard only checks for *an* authenticated session, not role — this change adds a stricter guard requiring `Role === 'Admin'`, reusing the same login flow, design tokens, and `AppShell` shipped in `add-player-web-ui` rather than building a parallel design system.
- **Explicitly out of scope**: every other admin screen named in `add-player-web-ui`'s design.md (D9) — game/template management, ad-hoc games, roster add/remove, waitlist promotion, payment reconciliation, admin balance list. This change is scoped to the import wizard only. Also out of scope: merging a duplicate person's history into an existing account — a collision is blocked, not resolved automatically.

## Capabilities

### New Capabilities
- `admin-roster-import-ui`: the admin-facing import wizard — CSV upload, flagged-row review with email/phone assignment, and submit (resolve + invite), plus the role-gated admin route group it lives in.

### Modified Capabilities
<!-- None — this change is purely additive on the frontend and consumes the existing, already-implemented roster-import and user-accounts (invite) capabilities without changing their requirements. -->

## Impact

- **New implementation in `src/web`**: an `admin/` route gated on `Role === 'Admin'` (distinct from `add-player-web-ui`'s any-authenticated-user guard), and wizard components (drop zone, review table, submit summary) built on the existing design-token system and `AppShell`.
- **Small, targeted change to `src/api`**: `ResolveFlaggedRowEndpoint` gains a pre-check for an existing player by email, returning `409` instead of a raw DB error. No new endpoints — `/admin/import/roster`, `/admin/import/flagged-rows`, and `/admin/invites` are otherwise consumed exactly as they exist today.
- **No breaking changes** to any archived or active capability.
