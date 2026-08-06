# notifications

## Purpose
TBD - defines how players are notified of league events via in-app notifications and PWA Web Push.

## Requirements

### Requirement: In-app notification list as baseline
The system SHALL record every notification event in a per-user in-app notification list that the user sees when they open the app, independent of any push delivery.

#### Scenario: User opens the app after an event
- **WHEN** a notification-triggering event occurred while the user was away
- **THEN** the user sees the notification in their in-app list on opening the app

### Requirement: PWA push delivery
The system SHALL deliver notifications via PWA Web Push to users who have granted push permission, in addition to recording them in the in-app list.

#### Scenario: Push to a subscribed user
- **WHEN** a notification event occurs and the user has an active push subscription
- **THEN** the system sends a Web Push message and also records the notification in the in-app list

#### Scenario: Push unavailable falls back to in-app
- **WHEN** a notification event occurs and the user has no active push subscription (e.g. iOS without home-screen install)
- **THEN** the system still records the notification in the in-app list so the user sees it on next open

### Requirement: Waitlist promotion notification
The system SHALL notify a player when an Admin promotes them from the waitlist into the game roster.

#### Scenario: Player promoted
- **WHEN** an Admin promotes a waitlisted player to the roster
- **THEN** the system notifies that player they are in the game

### Requirement: New-game / sign-ups-open notification
The system SHALL notify players when a new game becomes live and sign-ups open.

#### Scenario: Next game materializes
- **WHEN** a new game becomes the live game and sign-ups open
- **THEN** the system notifies players that sign-ups are open
