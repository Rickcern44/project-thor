## Why

`src/api`'s roster-import capability (spreadsheet upload → flag rows missing email/phone → admin resolves them → invite sent) is fully built and tested, but has no UI — an admin can only drive it via raw HTTP calls (curl/Postman), with an undocumented CSV column shape and no visibility into what got flagged or why. Now that the player app is live (`add-player-web-ui`), onboarding the season's actual roster is blocked on this being usable by a human, not just by someone reading the endpoint source.

## What Changes

- **New admin-only import wizard** in the existing `src/web` app: **Import → Review → Submit**.
  - **Import**: a drop zone for the CSV plus a season-year input, posted to the existing `POST /admin/import/roster`. Shows a summary of the result (games created, rows flagged, rows skipped as duplicates).
  - **Review**: lists every currently-pending flagged row (`GET /admin/import/flagged-rows`, not scoped to just this session's upload — it's a standing review queue), decoding each row's raw attendance/dues data for display. The admin confirms/edits the parsed name and fills in the email and phone the spreadsheet didn't have — both are required by `ResolveFlaggedRowEndpoint`, even though only email was explicitly asked for.
  - **Submit**: for each reviewed row, calls the existing `POST /admin/import/flagged-rows/{id}/resolve` (creates the real `Pending` `User` + `RosterRecord` + backfilled historical `SignUp`/`Charge` rows) and then `POST /admin/invites` for that same roster record, so the player's first login-link invite goes out in the same action. Each row's outcome (resolved+invited / failed) is reported individually, so one bad row doesn't block the rest.
- **New role-gated admin route group** in `src/web`: the existing `add-player-web-ui` auth guard only checks for *an* authenticated session, not role — this change adds a stricter guard requiring `Role === 'Admin'`, reusing the same login flow, design tokens, and `AppShell` shipped in `add-player-web-ui` rather than building a parallel design system.
- **Explicitly out of scope**: every other admin screen named in `add-player-web-ui`'s design.md (D9) — game/template management, ad-hoc games, roster add/remove, waitlist promotion, payment reconciliation, admin balance list. This change is scoped to the import wizard only.

## Capabilities

### New Capabilities
- `admin-roster-import-ui`: the admin-facing import wizard — CSV upload, flagged-row review with email/phone assignment, and submit (resolve + invite), plus the role-gated admin route group it lives in.

### Modified Capabilities
<!-- None — this change is purely additive on the frontend and consumes the existing, already-implemented roster-import and user-accounts (invite) capabilities without changing their requirements. -->

## Impact

- **New implementation in `src/web`**: an `(admin)` route group gated on `Role === 'Admin'` (distinct from `add-player-web-ui`'s any-authenticated-user guard), and wizard components (drop zone, review table, submit summary) built on the existing design-token system and `AppShell`.
- **No changes to `src/api`**: consumes `/admin/import/roster`, `/admin/import/flagged-rows`, `/admin/import/flagged-rows/{id}/resolve`, and `/admin/invites` exactly as they exist today.
- **No breaking changes** to any archived or active capability.
