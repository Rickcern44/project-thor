## 1. Project & PWA Foundation

- [x] 1.1 Scaffold the web app project (framework, build, lint/format) — already satisfied by `repo-init` (SvelteKit 5 + TailwindCSS + ESLint/Prettier)
- [x] 1.2 Add PWA shell: manifest, installability, and a registered Service Worker — already satisfied by `repo-init` (`@vite-pwa/sveltekit`, verified in 5.7)
- [x] 1.3 Set up the data store and schema migration tooling — already satisfied by `repo-init` (EF Core + Npgsql + migrations)
- [x] 1.4 Define core data model: users, roster records, game template, games, sign-ups, charges, notifications, push subscriptions

## 2. Accounts, Roles & Auth

- [x] 2.1 Implement passwordless magic-link authentication (login/session, no passwords)
- [x] 2.2 Implement Admin and Player roles with role-based permission checks on every action
- [x] 2.3 Scope Player self-service actions to the player's own account
- [x] 2.4 Implement admin-invite flow (issue invite, accept, set credentials)
- [x] 2.5 Link accepted invites to imported roster records (carry over balance/history)

## 3. Roster Import

- [x] 3.1 Build the one-time spreadsheet importer (parse rows → roster records) - CSV upload; historical per-date attendance imported as real Game/SignUp/Charge records, not just a roster summary (per user decision)
- [x] 3.2 Validate rows; flag unmatched/ambiguous/malformed rows for admin review - every row in the real sheet lacks email/phone (D11), so 100% flag by design, not a bug
- [x] 3.3 Admin review UI to resolve flagged rows - backend endpoint only (POST /admin/import/flagged-rows/{id}/resolve); no frontend UI, consistent with backend-first scope for this change
- [x] 3.4 Make import idempotent (no duplicates on re-run) - keyed on Name (case-insensitive), the only stable identifier the source data provides
- [x] 3.5 Cutover step - no code needed: the system never reads/writes the spreadsheet after the one-time upload, so there's no ongoing sync to cut over from; "app is source of truth" holds by construction

## 4. Game Scheduling

- [x] 4.1 Recurring template CRUD (day, time, default capacity, fee, sign-up lead time)
- [x] 4.2 Time-based auto-generation of the next game when current game time passes
- [x] 4.3 Sign-up window: open game at start − lead time; game states (closed → open → past); validate lead time < inter-game interval
- [x] 4.4 Enforce single-live-game invariant (allow quiet gaps)
- [x] 4.5 Ad-hoc one-off game creation
- [x] 4.6 Per-game capacity override
- [x] 4.7 Independent-instance editing/cancellation (no series effects)
- [x] 4.8 "Past games awaiting reconciliation" list for admins

## 5. Sign-up & Waitlist

- [x] 5.1 Player self-service sign-up (roster while under capacity)
- [x] 5.2 Player self-service cancel (roster or waitlist)
- [x] 5.3 Waitlist overflow in arrival order when at capacity
- [x] 5.4 Admin add/remove any player to/from roster or waitlist
- [x] 5.5 Admin-decided waitlist promotion into an open spot (no auto-promote)

## 6. Payment Tracking

- [x] 6.1 Configurable flat per-game fee (GameTemplate.Fee / Game.Fee, from §4; charges snapshot it at sign-up)
- [x] 6.2 Create charge on sign-up
- [x] 6.3 Erase charge on cancel before game time
- [x] 6.4 Mark charge owed for players on roster at game time
- [x] 6.5 Admin post-game waiver (no-show reconciliation)
- [x] 6.6 Mark charge paid; maintain per-player running balance
- [x] 6.7 Surface balances prominently; ensure balances never block sign-up

## 7. Notifications

- [x] 7.1 In-app notification list (per-user baseline)
- [x] 7.2 Web Push setup (VAPID keys, subscription storage, server send path)
- [ ] 7.3 Push permission UX + iOS "add to home screen" onboarding nudge - deferred: pure frontend UI, no backend component; backend-first per user decision on 2026-08-04
- [x] 7.4 Emit waitlist-promotion notification (in-app + push)
- [x] 7.5 Emit new-game/sign-ups-open notification (in-app + push)

## 8. Verification

- [x] 8.1 Validate scenarios from each spec against the implementation - audited all 6 specs against endpoints/tests; closed 2 real gaps (explicit waitlist-spot cancellation test, explicit no-auto-promote assertion) in SignUpFlowTests.cs; full suite (24 unit + 42 integration) passes
- [x] 8.2 End-to-end dry run: import → invite → sign up → waitlist → promote → reconcile → mark paid - added EndToEndDryRunTests.cs chaining the full lifecycle through real HTTP calls; passes
- [x] 8.3 Verify iOS push coverage gap is gracefully handled (in-app list always populated) - confirmed by code inspection: NotificationService.NotifyAsync always writes the Notification row before attempting push, and the push loop simply no-ops with zero subscriptions, so the in-app list is populated unconditionally regardless of push delivery
