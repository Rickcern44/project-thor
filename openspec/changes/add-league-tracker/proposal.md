## Why

A recreational basketball league currently runs sign-ups, waitlists, and dues out of a shared spreadsheet edited by hand. That works until two people edit at once, a waitlist spot opens and nobody notices, or someone's balance drifts out of sync. This change introduces a purpose-built, installable web app (PWA) that becomes the single source of truth: players sign themselves up on their phones, admins manage the roster and settle up per game, and the spreadsheet is retired after a one-time import.

## What Changes

- **New installable PWA** (responsive web, home-screen install) as the league's system of record.
- **Two roles — Admin and Player** — with admin-invite-only account creation (no open registration).
- **One-time roster import** from the league's existing spreadsheet, after which the spreadsheet is retired. **BREAKING** for the league's current workflow: the spreadsheet stops being authoritative.
- **Game scheduling** via a recurring template (day/time/capacity/fee defaults) plus ad-hoc one-off games. Exactly one game is live at a time; the next materializes automatically when the current game's time passes. Instances are independent (no recurring-series editing).
- **Self-service sign-up and cancellation** for players (own spot only); admins can add/remove any player to/from any game.
- **Waitlisting** with overflow beyond capacity and **admin-decided** promotion when a spot opens (no auto-promotion).
- **Per-game payment tracking**: a flat per-game fee attaches on sign-up, is erased on cancel, is owed if the player is on the roster at game time, and can be waived by an admin after the game (doubling as no-show/attendance reconciliation). Balances are tracked but never block future sign-ups.
- **Notifications**: an in-app notification list (reliable baseline) plus PWA push (best-effort) for waitlist promotion and new-game/sign-ups-open events.

## Capabilities

### New Capabilities

- `user-accounts`: Admin and Player roles, admin-invite-only account creation and onboarding, authentication, and role-based permissions.
- `roster-import`: One-time import of the existing league spreadsheet into the app's data store, linking imported records to invited accounts.
- `game-scheduling`: Recurring game template plus ad-hoc games, single-live-game model with time-based auto-generation of the next game, and per-game capacity.
- `game-signup`: Player self-service sign-up/cancel, admin add/remove, waitlist overflow, and admin-decided waitlist promotion.
- `payment-tracking`: Per-game fee lifecycle (charge on sign-up → erase on cancel → owed at game time → admin waive post-game), payment status, and per-player balance.
- `notifications`: In-app notification list plus PWA push delivery for the defined trigger events.

### Modified Capabilities

<!-- None — greenfield project, no existing specs. -->

## Impact

- **Greenfield build** — no existing code. Establishes the app's data model (users, games, sign-ups, charges, notifications), authentication, and PWA shell.
- **New infrastructure**: a Service Worker and Web Push (VAPID keys + a server-side send path) for push; a transactional data store; an installable PWA manifest.
- **Data migration**: a one-time importer that reads the league spreadsheet. The spreadsheet is decommissioned as the source of truth once import is verified.
- **iOS caveat**: Web Push on iOS only functions after a player installs the PWA to their home screen, so push coverage is best-effort and the in-app list is the guaranteed baseline.
