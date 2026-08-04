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

- [ ] 3.1 Build the one-time spreadsheet importer (parse rows → roster records)
- [ ] 3.2 Validate rows; flag unmatched/ambiguous/malformed rows for admin review
- [ ] 3.3 Admin review UI to resolve flagged rows
- [ ] 3.4 Make import idempotent (no duplicates on re-run)
- [ ] 3.5 Cutover step: mark app as source of truth, retire spreadsheet

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

- [ ] 5.1 Player self-service sign-up (roster while under capacity)
- [ ] 5.2 Player self-service cancel (roster or waitlist)
- [ ] 5.3 Waitlist overflow in arrival order when at capacity
- [ ] 5.4 Admin add/remove any player to/from roster or waitlist
- [ ] 5.5 Admin-decided waitlist promotion into an open spot (no auto-promote)

## 6. Payment Tracking

- [ ] 6.1 Configurable flat per-game fee
- [ ] 6.2 Create charge on sign-up
- [ ] 6.3 Erase charge on cancel before game time
- [ ] 6.4 Mark charge owed for players on roster at game time
- [ ] 6.5 Admin post-game waiver (no-show reconciliation)
- [ ] 6.6 Mark charge paid; maintain per-player running balance
- [ ] 6.7 Surface balances prominently; ensure balances never block sign-up

## 7. Notifications

- [ ] 7.1 In-app notification list (per-user baseline)
- [ ] 7.2 Web Push setup (VAPID keys, subscription storage, server send path)
- [ ] 7.3 Push permission UX + iOS "add to home screen" onboarding nudge
- [ ] 7.4 Emit waitlist-promotion notification (in-app + push)
- [ ] 7.5 Emit new-game/sign-ups-open notification (in-app + push)

## 8. Verification

- [ ] 8.1 Validate scenarios from each spec against the implementation
- [ ] 8.2 End-to-end dry run: import → invite → sign up → waitlist → promote → reconcile → mark paid
- [ ] 8.3 Verify iOS push coverage gap is gracefully handled (in-app list always populated)
