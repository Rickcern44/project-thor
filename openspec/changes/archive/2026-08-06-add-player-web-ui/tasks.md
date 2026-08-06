## 1. Design System Foundation

- [x] 1.1 Configure Tailwind v4 `@theme` tokens (color, accent, semantic, spacing) in `app.css`, dark values as default (D1)
- [x] 1.2 Add `@custom-variant dark` repointing `dark:` to `[data-theme="dark"]` instead of `prefers-color-scheme` (D1)
- [x] 1.3 Add light theme token overrides under `[data-theme="light"]`, re-tuned per token (not inverted) per the exploration mockup (D1)
- [x] 1.4 Add `@lucide/svelte` dependency (D6) — `lucide-svelte` is deprecated upstream in favor of this scoped package; same icon set/API
- [x] 1.5 Build the `AppShell` component: sidebar above the desktop breakpoint, bottom tab bar below it, via a `@container` query on the shell's own width, not a viewport media query (D2)
- [x] 1.6 Build the `ThemeToggle` component and the blocking inline theme-init script in `app.html` (reads `localStorage`, falls back to `prefers-color-scheme`, sets `data-theme` before first paint) (D5)
- [x] 1.7 Playwright test: shell renders bottom tab bar under the desktop breakpoint and a sidebar at/above it
- [x] 1.8 Playwright test: theme toggle switches the interface and persists across a reload

## 2. API Client & Auth Guard

- [x] 2.1 Build a typed fetch wrapper for the api origin, sending `credentials: 'include'` on every request (D7)
- [x] 2.2 Implement an auth guard for protected routes (`/`, `/balance`, `/notifications`, `/profile`): `export const ssr = false`, redirect to `/login` when `GET /auth/me` fails (D7)
- [x] 2.3 Playwright test: unauthenticated visitor hitting a protected route is redirected to `/login`

## 3. Authentication

- [x] 3.1 `/login` route: email form calling `POST /auth/login/request`, confirmation state on submit
- [x] 3.2 Magic-link consume route: calls `POST /auth/consume` with the token, establishes session, redirects to `/` on success
- [x] 3.3 Logout action on `/profile` calling `POST /auth/logout`
- [x] 3.4 Playwright test: requesting a login link shows the confirmation state
- [x] 3.5 Playwright test: consuming a valid link authenticates and lands on the live game screen

## 4. Live Game, Sign-Up & Waitlist

- [x] 4.1 `GameCard` component: date/time (no location — `Game`/`GameResponse` has no location field in the backend; omitted, see design note), fee, roster count vs. capacity, status badge
- [x] 4.2 Live game screen (`/`): fetch `GET /games/live`, render `GameCard`; clear empty state when no live game exists
- [x] 4.3 Sign-up action: `POST /games/{id}/signup`, update the card to rostered or waitlisted based on the response
- [x] 4.4 Waitlist position display when waitlisted
- [x] 4.5 Cancel action: `POST /games/{id}/cancel`, update the card to reflect no active sign-up
- [x] 4.6 Playwright test: signing up while open and under capacity shows rostered state
- [x] 4.7 Playwright test: signing up at capacity shows waitlisted state with position
- [x] 4.8 Playwright test: cancel removes the sign-up and updates the card
- [x] 4.9 Playwright test: no live game renders the empty state, not a broken or blank card

## 5. Balance

- [x] 5.1 Balance screen: fetch `GET /players/{playerUserId}/balance` for the signed-in player, render the current balance including zero/credit
- [x] 5.2 Playwright test: balance screen shows the current balance

## 6. Notifications

- [x] 6.1 Notification list: fetch `GET /notifications`, render with read/unread visually distinguished
- [x] 6.2 Unread indicator surfaced from the nav (sidebar and tab bar)
- [x] 6.3 Mark-as-read action: `POST /notifications/{id}/read`, updates the indicator
- [x] 6.4 Playwright test: unread indicator shows with unread notifications; opening one marks it read and the indicator updates

## 7. Profile

- [x] 7.1 Minimal profile screen: signed-in player's name/email, logout button

## 8. Verification

- [x] 8.1 Validate every scenario in `specs/player-web-ui/spec.md` against the actual implementation
- [x] 8.2 Manual check: resize a desktop-width window down through the breakpoint without reloading; confirm the shell reflows correctly
- [x] 8.3 Manual check: toggle theme, reload, confirm the choice persists with no flash of the wrong theme
- [x] 8.4 Confirm `src/web` CI (`npm run check`, `npm run lint`, Playwright suite) passes end to end

## 9. Local Dev Conveniences (out of scope, ad-hoc — src/api)

Surfaced while manually testing the login flow: `src/api` had no way to see a magic link locally
(real Resend delivery only) and no way to reach an Active user at all on a fresh database. Neither
is part of this change's spec — both are local-dev-only, gated by `IsDevelopment()`, and don't touch
production behavior.

- [x] 9.1 `ConsoleEmailSender`: `IEmailSender` swapped in for `Development` only, logs the recipient/subject/bare link (regex-extracted from the HTML body) instead of calling Resend, so magic links show up as a structured log entry (visible in Aspire's dashboard)
- [x] 9.2 Dev seed in `Program.cs`: creates an Active `player@example.com` (Player) and `admin@example.com` (Admin) user on startup if they don't already exist, so there's something to log in as on a fresh database without manual SQL
