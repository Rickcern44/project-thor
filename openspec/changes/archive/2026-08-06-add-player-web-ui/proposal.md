## Why

The backend is fully built — auth, game scheduling, sign-ups, payment tracking, and notifications are all implemented and tested (`add-league-tracker`, archived) — but `src/web` is still the scaffold placeholder from `repo-init`: a static page reading "League scheduling and payment tracking, in progress." Players have no way to actually use the app. This change delivers the real player-facing UI so the league can start signing up for games and tracking dues for real, starting with the highest-value slice rather than the full admin surface at once.

## What Changes

- **New responsive web UI**, mobile-first and installable, replacing the placeholder page. One component shell adapts via CSS container queries (not viewport media queries) so it works correctly both in a mobile browser and as an installed desktop PWA window of arbitrary size.
- **Dark-primary theme with a working light/dark toggle** — light mode is a properly re-tuned second theme (warm off-white ground, deepened accent/semantic colors for contrast), not a naive inversion of dark.
- **Sidebar navigation** on wide/desktop widths, collapsing to a bottom tab bar on mobile widths, sharing the same markup and design tokens.
- **Passwordless magic-link login**: request-link and consume-link flows against the existing `/auth/login/request` and `/auth/consume` endpoints, establishing a session.
- **Live game view**: current game status (closed / open / past), roster count and capacity, fee, sign-up and cancel actions, waitlist position when applicable.
- **Balance view**: the signed-in player's running balance.
- **In-app notification list**, including read/unread state.
- **Explicitly out of scope for this change**: all admin screens (roster import, game template/ad-hoc game management, roster add/remove, waitlist promotion, payment reconciliation/waive/pay, admin balance list) — deferred to a follow-up change, since that surface is much larger and was only loosely explored, not designed in detail. Web Push permission UX / iOS "add to home screen" onboarding nudge remains deferred for the same reason it was deferred in `add-league-tracker` (task 7.3): pure frontend UI with no backend component, lower priority than the core sign-up flow.

## Capabilities

### New Capabilities
- `player-web-ui`: The player-facing web application — responsive shell (sidebar/tab-bar navigation, light/dark theming), authentication flow, live game sign-up/cancel/waitlist, balance display, and in-app notifications.

### Modified Capabilities
<!-- None — this change is purely additive on the frontend and consumes existing backend capabilities (user-accounts, game-scheduling, game-signup, payment-tracking, notifications) without changing their requirements. -->

## Impact

- **New implementation in `src/web`**: replaces the placeholder route tree with a real component-based frontend (SvelteKit 5). No changes to `src/api` — this change consumes the existing, already-implemented API surface (`/auth/*`, `/games/live`, `/games/{id}/roster`, `/games/{id}/signup`, `/games/{id}/cancel`, `/players/{id}/balance`, `/notifications`) as-is.
- **New frontend infrastructure**: a design-token system (color, type, spacing) implemented via Tailwind v4's CSS-based `@theme`, since `src/web` currently has no custom theme configuration at all.
- **No breaking changes** to any archived capability or API contract.
