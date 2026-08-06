## 1. Auth & Shell Foundation

- [x] 1.1 Build `src/routes/admin/+layout.ts` guard: `export const ssr = false`, calls `GET /auth/me`, redirects unauthenticated visitors to `/login` and authenticated non-admins to `/` (D1) — a plain `admin/` folder, not a `(admin)` route *group*: groups are invisible in the URL, and this needed the literal `/admin/import` path
- [x] 1.2 Build a minimal `AdminShell` component: title bar + logout, not the player `AppShell` (D2)
- [x] 1.3 Add a conditional "Admin" nav entry to the existing `AppShell`, shown only when the signed-in user's role is Admin, linking to `/admin/import` (D3)
- [x] 1.4 Playwright test: an authenticated non-admin visiting `/admin/import` is denied/redirected
- [x] 1.5 Playwright test: an unauthenticated visitor hitting `/admin/import` is redirected to `/login`

## 2. Import Step

- [x] 2.1 Build a `DropZone` component: drag-and-drop plus a real `<input type="file" accept=".csv">` fallback; rejects a non-`.csv` selection client-side with a clear message before any upload (D5)
- [x] 2.2 Season-year input alongside the file selection
- [x] 2.3 Wire submission to the existing `POST /admin/import/roster`; show the resulting games-created / rows-flagged / rows-skipped-as-duplicate counts
- [x] 2.4 Playwright test: a successful import shows the summary counts
- [x] 2.5 Playwright test: selecting a non-`.csv` file is rejected before any upload call

## 3. Review Step

- [x] 3.1 Extend `lib/api/client.ts` with typed wrappers for `GET /admin/import/flagged-rows`, `POST /admin/import/flagged-rows/{id}/resolve`, and `POST /admin/invites`
- [x] 3.2 Fetch pending flagged rows and decode each row's `RawData` JSON (`Name`, `AttendedDates`, `TotalDue`, `AmountPaid`) for display (D6) — note: `RawData` is PascalCase JSON (a plain `JsonSerializer.Serialize` call server-side, not the Web-defaults HTTP pipeline), unlike every other camelCase response on this client
- [x] 3.3 Review table: editable Name, required Email and Phone inputs; read-only attended dates, total due, and amount paid (D6)
- [x] 3.4 Client-side validation: block submission of a row missing email or phone, indicating the missing field — implemented as a per-row check inside the single "Submit" action (skips the API calls and marks that row `error` instead of blocking the whole batch), consistent with D7's per-row independent outcome
- [x] 3.5 Playwright test: the review list shows each pending row's parsed name, attended dates, total due, and amount paid
- [x] 3.6 Playwright test: a row missing email or phone cannot be submitted

## 4. Submit Step

- [x] 4.1 Submit reviewed rows sequentially, each as resolve → invite (`POST .../resolve` then `POST /admin/invites` for the resulting `rosterRecordId`), tracking per-row status (`pending` / `success` / `error`) (D7, D8)
- [x] 4.2 Render each row's outcome as it completes; one row's failure does not block submission of the remaining rows
- [x] 4.3 Playwright test: submitting a fully reviewed row resolves it and sends its invite, shown as succeeded
- [x] 4.4 Playwright test: one row's submission failure is reported without blocking the remaining rows' submission

## 5. Verification

- [x] 5.1 Validate every scenario in `specs/admin-roster-import-ui/spec.md` against the actual implementation
- [x] 5.2 Confirm `src/web` CI (`npm run check`, `npm run lint`, Playwright suite) passes end to end

## 6. Row Selection

- [x] 6.1 Add a `selected` checkbox per review row (defaulting to unchecked) plus a header "select all" checkbox (D9)
- [x] 6.2 Submit processes only rows where `selected` is true and not already `success`; unselected rows are left untouched in the pending queue (D9)
- [x] 6.3 Playwright test: rows are unselected by default and submitting with nothing selected resolves/invites nothing
- [x] 6.4 Playwright test: selecting a subset and submitting only resolves/invites the selected rows, leaving the rest pending on the next review load

## 7. Duplicate Email Protection

- [ ] 7.1 `ResolveFlaggedRowEndpoint`: look up an existing `RosterRecord`/`User` by the submitted email before inserting; return `409 Conflict` with a clear message instead of letting the unique-constraint violation surface as a raw 500 (D10)
- [ ] 7.2 Frontend: surface that `409`'s message on the row via the existing per-row error path — no new error-handling UI needed
- [ ] 7.3 xUnit test (`ProjectThor.Api.UnitTests` or `IntegrationTests`): resolving a row whose email already belongs to an existing player returns `409`, not `500`, and does not create a second `User`/`RosterRecord`
- [ ] 7.4 Playwright test: a row whose email collides with an existing player is reported as failed without blocking the other selected rows

## 8. Verification (Update)

- [ ] 8.1 Re-validate every scenario in `specs/admin-roster-import-ui/spec.md` — including the selection and duplicate-email requirements — against the implementation
- [ ] 8.2 Confirm `src/web` CI (`npm run check`, `npm run lint`, Playwright suite) and `src/api` tests (`dotnet test`) both pass end to end
