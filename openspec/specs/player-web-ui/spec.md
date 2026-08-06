# player-web-ui

## Purpose
TBD - defines the player-facing web application: responsive navigation shell (sidebar/tab-bar), light/dark theming, passwordless magic-link login, live game display and sign-up, waitlist display and cancellation, balance display, and in-app notification list.

## Requirements

### Requirement: Responsive navigation shell
The system SHALL present a sidebar navigation pattern when the app's available width is at or above the desktop breakpoint, and a bottom tab bar when narrower, using the same navigation destinations in both, based on the app shell's own available width rather than the browser viewport.

#### Scenario: Narrow width shows a bottom tab bar
- **WHEN** the app shell's available width is below the desktop breakpoint
- **THEN** navigation is presented as a bottom tab bar

#### Scenario: Wide width shows a sidebar
- **WHEN** the app shell's available width is at or above the desktop breakpoint
- **THEN** navigation is presented as a persistent sidebar

### Requirement: Light/dark theme toggle
The system SHALL let a user switch between a light and a dark theme, and SHALL persist that choice on the same device across sessions.

#### Scenario: User toggles theme
- **WHEN** a user selects the light or dark theme option
- **THEN** the interface immediately re-themes and the choice is remembered on the next visit from the same device

#### Scenario: No stored preference yet
- **WHEN** a user opens the app for the first time on a device with no stored theme preference
- **THEN** the app follows the device's OS-level light/dark preference

### Requirement: Passwordless magic-link login
The system SHALL let a user request a login link by email and become authenticated by following it, consistent with the existing passwordless magic-link authentication capability.

#### Scenario: User requests a login link
- **WHEN** a user submits their registered email on the login screen
- **THEN** the system requests a magic link for that email and shows a confirmation that a link was sent

#### Scenario: User follows a valid login link
- **WHEN** a user follows a valid, unexpired magic link
- **THEN** the user is authenticated and lands on the live game screen

#### Scenario: Unauthenticated access redirects to login
- **WHEN** an unauthenticated visitor attempts to view any player screen other than login
- **THEN** the system redirects them to the login screen

### Requirement: Live game display and sign-up
The system SHALL show the authenticated player the current live game's status, roster, and fee, and SHALL let them sign up while capacity remains.

#### Scenario: Game is open for sign-up
- **WHEN** the live game is open for sign-up
- **THEN** the player sees the game's date, time, fee, and current roster count against capacity, with a sign-up action available

#### Scenario: Player signs up successfully
- **WHEN** a player selects the sign-up action while the game is open and under capacity
- **THEN** the player is added to the roster and the screen reflects their rostered status

#### Scenario: No live game exists
- **WHEN** there is no open or upcoming live game
- **THEN** the screen communicates that clearly rather than showing an empty or broken game card

### Requirement: Waitlist display and cancellation
The system SHALL show a player their waitlist position when waitlisted, and SHALL let a rostered or waitlisted player cancel their own spot.

#### Scenario: Player is waitlisted
- **WHEN** a player signs up for a game that is at capacity
- **THEN** the screen shows their waitlist position instead of a rostered state

#### Scenario: Player cancels
- **WHEN** a rostered or waitlisted player selects the cancel action
- **THEN** their sign-up is removed and the screen reflects that they are no longer signed up

### Requirement: Balance display
The system SHALL show the authenticated player their current running balance.

#### Scenario: Player views their balance
- **WHEN** a player opens the balance screen
- **THEN** the system displays their current outstanding balance, including zero or credit balances

### Requirement: In-app notification list
The system SHALL show the authenticated player their notifications, including read/unread state, consistent with the existing in-app notification list capability.

#### Scenario: Player has unread notifications
- **WHEN** a player has one or more unread notifications
- **THEN** an indicator is visible from the main navigation, and opening the notification list shows them with unread state visually distinguished from read ones

#### Scenario: Player reads a notification
- **WHEN** a player opens an unread notification
- **THEN** it is marked read and the unread indicator updates accordingly
