## 1. Auth & Shell Foundation

- [ ] 1.1 Build `(admin)` route group layout guard: `export const ssr = false`, calls `GET /auth/me`, redirects unauthenticated visitors to `/login` and authenticated non-admins to `/` (D1)
- [ ] 1.2 Build a minimal `AdminShell` component: title bar + logout, not the player `AppShell` (D2)
- [ ] 1.3 Add a conditional "Admin" nav entry to the existing `AppShell`, shown only when the signed-in user's role is Admin, linking to `/admin/import` (D3)
- [ ] 1.4 Playwright test: an authenticated non-admin visiting `/admin/import` is denied/redirected
- [ ] 1.5 Playwright test: an unauthenticated visitor hitting `/admin/import` is redirected to `/login`

## 2. Import Step

- [ ] 2.1 Build a `DropZone` component: drag-and-drop plus a real `<input type="file" accept=".csv">` fallback; rejects a non-`.csv` selection client-side with a clear message before any upload (D5)
- [ ] 2.2 Season-year input alongside the file selection
- [ ] 2.3 Wire submission to the existing `POST /admin/import/roster`; show the resulting games-created / rows-flagged / rows-skipped-as-duplicate counts
- [ ] 2.4 Playwright test: a successful import shows the summary counts
- [ ] 2.5 Playwright test: selecting a non-`.csv` file is rejected before any upload call

## 3. Review Step

- [ ] 3.1 Extend `lib/api/client.ts` with typed wrappers for `GET /admin/import/flagged-rows`, `POST /admin/import/flagged-rows/{id}/resolve`, and `POST /admin/invites`
- [ ] 3.2 Fetch pending flagged rows and decode each row's `RawData` JSON (`Name`, `AttendedDates`, `TotalDue`, `AmountPaid`) for display (D6)
- [ ] 3.3 Review table: editable Name, required Email and Phone inputs; read-only attended dates, total due, and amount paid (D6)
- [ ] 3.4 Client-side validation: block submission of a row missing email or phone, indicating the missing field
- [ ] 3.5 Playwright test: the review list shows each pending row's parsed name, attended dates, total due, and amount paid
- [ ] 3.6 Playwright test: a row missing email or phone cannot be submitted

## 4. Submit Step

- [ ] 4.1 Submit reviewed rows sequentially, each as resolve → invite (`POST .../resolve` then `POST /admin/invites` for the resulting `rosterRecordId`), tracking per-row status (`pending` / `success` / `error`) (D7, D8)
- [ ] 4.2 Render each row's outcome as it completes; one row's failure does not block submission of the remaining rows
- [ ] 4.3 Playwright test: submitting a fully reviewed row resolves it and sends its invite, shown as succeeded
- [ ] 4.4 Playwright test: one row's submission failure is reported without blocking the remaining rows' submission

## 5. Verification

- [ ] 5.1 Validate every scenario in `specs/admin-roster-import-ui/spec.md` against the actual implementation
- [ ] 5.2 Confirm `src/web` CI (`npm run check`, `npm run lint`, Playwright suite) passes end to end
